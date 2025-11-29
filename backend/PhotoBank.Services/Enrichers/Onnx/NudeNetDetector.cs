using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagick;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoBank.Services.Onnx.Base;
using PhotoBank.Services.Onnx.Models;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// NudeNet detector using ONNX Runtime and YOLOv8-based model
/// Thread-safe for concurrent use
/// </summary>
public interface INudeNetDetector : IDisposable
{
    /// <summary>
    /// Detects NSFW content using pre-prepared letterboxed 640x640 image.
    /// </summary>
    /// <param name="letterboxedImage">Pre-prepared 640x640 letterboxed image.</param>
    /// <param name="originalWidth">Original image width (before letterboxing).</param>
    /// <param name="originalHeight">Original image height (before letterboxing).</param>
    /// <param name="letterboxScale">Scale factor used for letterboxing.</param>
    /// <param name="padX">Horizontal padding used for letterboxing.</param>
    /// <param name="padY">Vertical padding used for letterboxing.</param>
    NudeNetDetectionResult Detect(
        IMagickImage<byte> letterboxedImage,
        int originalWidth,
        int originalHeight,
        float letterboxScale,
        int padX,
        int padY);
}

public class NudeNetDetector : OnnxInferenceServiceBase, INudeNetDetector
{
    private readonly NudeNetOnnxOptions _options;
    private readonly int _inputSize;
    private const int NumClasses = 18; // NudeNet has 18 classes

    // YOLOv8 constants for 320n model (most common)
    // For 320x320: predictions = 1600 + 400 + 100 = 2100
    // For 640x640: predictions = 6400 + 1600 + 400 = 8400
    private const int YoloV8OutputSize320 = 84; // Not used, but for reference: 4 bbox + 80 COCO classes
    private const int NudeNetOutputSize = 22; // 4 bbox + 18 NudeNet classes

    public NudeNetDetector(IOptions<NudeNetOnnxOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _inputSize = _options.InputResolution;

        if (_inputSize != 320 && _inputSize != 640)
            throw new ArgumentException($"Invalid input resolution: {_inputSize}. Must be 320 or 640.");

        // Initialize ONNX session with CUDA GPU acceleration
        InitializeSession(_options.ModelPath, useCuda: true, cudaDeviceId: 0);
    }

    public NudeNetDetectionResult Detect(
        IMagickImage<byte> letterboxedImage,
        int originalWidth,
        int originalHeight,
        float letterboxScale,
        int padX,
        int padY)
    {
        if (letterboxedImage == null)
            throw new ArgumentNullException(nameof(letterboxedImage));

        if (letterboxedImage.Width != _inputSize || letterboxedImage.Height != _inputSize)
            throw new ArgumentException($"Expected letterboxed image size {_inputSize}x{_inputSize}, but got {letterboxedImage.Width}x{letterboxedImage.Height}");

        // Convert letterboxed image to tensor
        var inputData = ImageToTensor(letterboxedImage);

        // Create tensor from input data [1, 3, size, size]
        var tensor = new DenseTensor<float>(inputData, new[] { 1, 3, _inputSize, _inputSize });

        // Run inference using base class method (thread-safe for concurrent operations)
        var outputArray = ExecuteInference("images", tensor);

        if (outputArray == null || outputArray.Length == 0)
            return CreateEmptyResult();

        // Parse YOLOv8 output format
        var detections = ParseYoloV8Output(outputArray, originalWidth, originalHeight, letterboxScale, padX, padY);

        // Apply NMS to filter overlapping detections
        var filteredDetections = ApplyNMS(detections, _options.NmsThreshold);

        // Classify as NSFW/Racy based on detected classes
        return ClassifyDetections(filteredDetections);
    }

    /// <summary>
    /// Converts letterboxed image to tensor in CHW format (Channel, Height, Width).
    /// Normalizes pixel values from [0, 255] to [0, 1].
    /// </summary>
    private float[] ImageToTensor(IMagickImage<byte> letterboxedImage)
    {
        // Ensure RGB format
        letterboxedImage.ColorSpace = ColorSpace.sRGB;

        // Convert to CHW format (Channel, Height, Width) and normalize [0, 255] -> [0, 1]
        var input = new float[1 * 3 * _inputSize * _inputSize];
        var pixels = letterboxedImage.GetPixels();

        var index = 0;
        for (int c = 0; c < 3; c++) // RGB channels
        {
            for (int y = 0; y < _inputSize; y++)
            {
                for (int x = 0; x < _inputSize; x++)
                {
                    var pixel = pixels.GetPixel(x, y);
                    input[index++] = c switch
                    {
                        0 => pixel.GetChannel(0) / 255f,  // R
                        1 => pixel.GetChannel(1) / 255f,  // G
                        2 => pixel.GetChannel(2) / 255f,  // B
                        _ => 0
                    };
                }
            }
        }

        return input;
    }

    /// <summary>
    /// Parse YOLOv8 output: [1, 22, N] channels-first layout where N depends on input size
    /// For 320x320: N ≈ 2100, for 640x640: N = 8400
    /// </summary>
    private List<DetectedObjectOnnx> ParseYoloV8Output(float[] output, int originalWidth, int originalHeight, float scale, int padX, int padY)
    {
        var detections = new List<DetectedObjectOnnx>();

        // Determine number of predictions from output array length
        // Output format: [1, 22, N] = 22 * N elements
        var numPredictions = output.Length / NudeNetOutputSize;

        for (int i = 0; i < numPredictions; i++)
        {
            // Read bbox coordinates using channel stride
            var centerX = output[0 * numPredictions + i];  // Channel 0
            var centerY = output[1 * numPredictions + i];  // Channel 1
            var width = output[2 * numPredictions + i];    // Channel 2
            var height = output[3 * numPredictions + i];   // Channel 3

            // Find max class score across 18 classes
            var maxClassScore = 0f;
            var maxClassIndex = -1;

            for (int classIdx = 0; classIdx < NumClasses; classIdx++)
            {
                var classScore = output[(4 + classIdx) * numPredictions + i];
                if (classScore > maxClassScore)
                {
                    maxClassScore = classScore;
                    maxClassIndex = classIdx;
                }
            }

            // Filter by confidence threshold
            if (maxClassScore < _options.ConfidenceThreshold || maxClassIndex < 0)
                continue;

            // Convert from letterboxed coordinates to original image coordinates
            var centerXScaled = centerX - padX;
            var centerYScaled = centerY - padY;

            var x = (centerXScaled - width / 2) / scale;
            var y = (centerYScaled - height / 2) / scale;
            var w = width / scale;
            var h = height / scale;

            // Clamp coordinates to image bounds
            var clampedX = Math.Max(0, Math.Min(x, originalWidth));
            var clampedY = Math.Max(0, Math.Min(y, originalHeight));

            var rightEdge = Math.Min(x + w, originalWidth);
            var bottomEdge = Math.Min(y + h, originalHeight);

            var clampedW = Math.Max(0, rightEdge - clampedX);
            var clampedH = Math.Max(0, bottomEdge - clampedY);

            detections.Add(new DetectedObjectOnnx
            {
                ClassName = NudeNetClassNames.Names[maxClassIndex],
                Confidence = maxClassScore,
                X = clampedX,
                Y = clampedY,
                Width = clampedW,
                Height = clampedH
            });
        }

        return detections;
    }

    /// <summary>
    /// Applies class-aware Non-Maximum Suppression (NMS) to filter overlapping detections
    /// </summary>
    private List<DetectedObjectOnnx> ApplyNMS(List<DetectedObjectOnnx> detections, float nmsThreshold)
    {
        var result = new List<DetectedObjectOnnx>();

        // Group detections by class name and apply NMS per class
        var groupedByClass = detections.GroupBy(d => d.ClassName);

        foreach (var classGroup in groupedByClass)
        {
            var classDetections = classGroup.OrderByDescending(d => d.Confidence).ToList();

            while (classDetections.Count > 0)
            {
                var best = classDetections[0];
                result.Add(best);
                classDetections.RemoveAt(0);

                // Remove boxes that overlap significantly with the best box
                classDetections = classDetections
                    .Where(d => CalculateIoU(best, d) < nmsThreshold)
                    .ToList();
            }
        }

        return result;
    }

    private static float CalculateIoU(DetectedObjectOnnx box1, DetectedObjectOnnx box2)
    {
        var x1 = Math.Max(box1.X, box2.X);
        var y1 = Math.Max(box1.Y, box2.Y);
        var x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width);
        var y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);

        var intersectionArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var box1Area = box1.Width * box1.Height;
        var box2Area = box2.Width * box2.Height;
        var unionArea = box1Area + box2Area - intersectionArea;

        return unionArea > 0 ? intersectionArea / unionArea : 0;
    }

    /// <summary>
    /// Classify detections as NSFW or Racy based on detected classes and thresholds
    /// </summary>
    private NudeNetDetectionResult ClassifyDetections(List<DetectedObjectOnnx> detections)
    {
        if (detections.Count == 0)
            return CreateEmptyResult();

        var result = new NudeNetDetectionResult
        {
            Detections = detections
        };

        // Count detections by class
        result.DetectionCounts = detections
            .GroupBy(d => d.ClassName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Find max confidence for explicit classes
        var explicitDetections = detections
            .Where(d => NudeNetClassNames.ExplicitClasses.Contains(d.ClassName))
            .ToList();

        if (explicitDetections.Any())
        {
            var maxExplicitConfidence = explicitDetections.Max(d => d.Confidence);
            result.NsfwConfidence = maxExplicitConfidence;
            result.IsNsfw = maxExplicitConfidence >= _options.ExplicitThreshold;
        }

        // Find max confidence for racy classes (only if not already NSFW)
        var racyDetections = detections
            .Where(d => NudeNetClassNames.RacyClasses.Contains(d.ClassName))
            .ToList();

        if (racyDetections.Any())
        {
            var maxRacyConfidence = racyDetections.Max(d => d.Confidence);
            result.RacyConfidence = maxRacyConfidence;

            // Only mark as racy if not already NSFW
            if (!result.IsNsfw)
            {
                result.IsRacy = maxRacyConfidence >= _options.RacyThreshold;
            }
        }

        return result;
    }

    private static NudeNetDetectionResult CreateEmptyResult()
    {
        return new NudeNetDetectionResult
        {
            IsNsfw = false,
            NsfwConfidence = 0f,
            IsRacy = false,
            RacyConfidence = 0f,
            Detections = new List<DetectedObjectOnnx>(),
            DetectionCounts = new Dictionary<string, int>()
        };
    }

    // Dispose is inherited from OnnxInferenceServiceBase
}
