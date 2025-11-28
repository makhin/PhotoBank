using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using ImageMagick;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoBank.Services.Onnx.Base;
using PhotoBank.Services.Onnx.Models;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Service for YOLO ONNX model inference (thread-safe)
/// </summary>
public interface IYoloOnnxService : IDisposable
{
    List<DetectedObjectOnnx> DetectObjects(IMagickImage<byte> image, float confidenceThreshold = 0.5f, float nmsThreshold = 0.45f);
}

/// <summary>
/// YOLO output format type
/// </summary>
internal enum YoloFormat
{
    Unknown,
    YoloV5,  // [1, 25200, 85] - boxes-first layout
    YoloV8   // [1, 84, 8400] - channels-first layout
}

/// <summary>
/// Thread-safe YOLO service using ONNX Runtime with CUDA GPU acceleration
/// Note: InferenceSession is thread-safe for concurrent inference operations
/// </summary>
public class YoloOnnxService : OnnxInferenceServiceBase, IYoloOnnxService
{
    private const int InputWidth = 640;
    private const int InputHeight = 640;
    private const int NumClasses = 80;

    // YOLOv5 constants
    private const int YoloV5NumPredictions = 25200;
    private const int YoloV5OutputSize = 85; // 4 bbox + 1 objectness + 80 classes

    // YOLOv8 constants
    private const int YoloV8NumPredictions = 8400;
    private const int YoloV8OutputSize = 84; // 4 bbox + 80 classes (no objectness)

    public YoloOnnxService(IOptions<YoloOnnxOptions> options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        var opts = options.Value;

        // Initialize ONNX session with CUDA GPU acceleration
        // Validation is handled by the base class (OnnxSessionFactory)
        InitializeSession(opts.ModelPath, useCuda: true, cudaDeviceId: 0);
    }

    public List<DetectedObjectOnnx> DetectObjects(IMagickImage<byte> image, float confidenceThreshold = 0.5f, float nmsThreshold = 0.45f)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        var originalWidth = (int)image.Width;
        var originalHeight = (int)image.Height;

        // Prepare input with letterboxing (preserve aspect ratio + padding)
        var (inputData, scale, padX, padY) = PrepareInputWithLetterbox(image);

        // Create tensor from input data [1, 3, 640, 640]
        var tensor = new DenseTensor<float>(inputData, new[] { 1, 3, InputHeight, InputWidth });

        // Run inference using base class method (thread-safe for concurrent operations)
        var outputArray = ExecuteInference("images", tensor);

        if (outputArray == null || outputArray.Length == 0)
            return new List<DetectedObjectOnnx>();

        // Detect YOLO format and parse output accordingly
        var format = DetectYoloFormat(outputArray);
        var detections = format switch
        {
            YoloFormat.YoloV5 => ParseYoloV5Output(outputArray, originalWidth, originalHeight, scale, padX, padY, confidenceThreshold),
            YoloFormat.YoloV8 => ParseYoloV8Output(outputArray, originalWidth, originalHeight, scale, padX, padY, confidenceThreshold),
            _ => throw new NotSupportedException($"Unsupported YOLO output format. Output array length: {outputArray.Length}")
        };

        var filteredDetections = ApplyNMS(detections, nmsThreshold);

        return filteredDetections;
    }

    private static YoloFormat DetectYoloFormat(float[] output)
    {
        var length = output.Length;

        // YOLOv8: [1, 84, 8400] = 705,600 elements (channels-first)
        if (length == YoloV8OutputSize * YoloV8NumPredictions)
            return YoloFormat.YoloV8;

        // YOLOv5: [1, 25200, 85] = 2,142,000 elements (boxes-first)
        if (length == YoloV5NumPredictions * YoloV5OutputSize)
            return YoloFormat.YoloV5;

        return YoloFormat.Unknown;
    }

    /// <summary>
    /// Prepare input with letterboxing: resize preserving aspect ratio and add padding.
    /// Returns the input tensor and letterbox parameters (scale, padX, padY) for coordinate conversion.
    /// </summary>
    private static (float[] input, float scale, int padX, int padY) PrepareInputWithLetterbox(IMagickImage<byte> image)
    {
        var originalWidth = (int)image.Width;
        var originalHeight = (int)image.Height;

        // Calculate scale to fit image into 640x640 while preserving aspect ratio
        var scale = Math.Min((float)InputWidth / originalWidth, (float)InputHeight / originalHeight);

        // Calculate new dimensions after scaling
        var newWidth = (uint)(originalWidth * scale);
        var newHeight = (uint)(originalHeight * scale);

        // Calculate padding to center the image in 640x640
        var padX = (InputWidth - (int)newWidth) / 2;
        var padY = (InputHeight - (int)newHeight) / 2;

        // Create letterboxed image (640x640 with black padding)
        using var letterboxed = new MagickImage(MagickColors.Black, (uint)InputWidth, (uint)InputHeight);

        // Resize original image preserving aspect ratio
        using var resized = image.Clone();
        resized.Resize(newWidth, newHeight);

        // Copy resized image to center of letterboxed canvas
        letterboxed.Composite(resized, padX, padY, CompositeOperator.Over);

        // Ensure RGB format
        letterboxed.ColorSpace = ColorSpace.sRGB;

        // Convert to CHW format (Channel, Height, Width) and normalize [0, 255] -> [0, 1]
        var input = new float[1 * 3 * InputHeight * InputWidth];
        var pixels = letterboxed.GetPixels();

        var index = 0;
        for (int c = 0; c < 3; c++) // RGB channels
        {
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
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

        return (input, scale, padX, padY);
    }

    /// <summary>
    /// Parse YOLOv5 output: [1, 25200, 85] boxes-first layout
    /// Each prediction: [centerX, centerY, width, height, objectness, class0_score, ..., class79_score]
    /// </summary>
    private static List<DetectedObjectOnnx> ParseYoloV5Output(float[] output, int originalWidth, int originalHeight, float scale, int padX, int padY, float confidenceThreshold)
    {
        var detections = new List<DetectedObjectOnnx>();

        // YOLOv5 output format: [1, 25200, 85]
        // Layout: boxes-first, where each of 25200 predictions has 85 consecutive values
        // [centerX, centerY, width, height, objectness, class0, class1, ..., class79]
        // To access prediction i, field f: output[i * 85 + f]

        for (int i = 0; i < YoloV5NumPredictions; i++)
        {
            var offset = i * YoloV5OutputSize;

            // Read bbox coordinates
            var centerX = output[offset + 0];
            var centerY = output[offset + 1];
            var width = output[offset + 2];
            var height = output[offset + 3];
            var objectness = output[offset + 4];

            // Find max class score across 80 classes
            var maxClassScore = 0f;
            var maxClassIndex = -1;

            for (int classIdx = 0; classIdx < NumClasses; classIdx++)
            {
                var classScore = output[offset + 5 + classIdx];
                if (classScore > maxClassScore)
                {
                    maxClassScore = classScore;
                    maxClassIndex = classIdx;
                }
            }

            // YOLOv5 uses objectness * class_score as final confidence
            var confidence = objectness * maxClassScore;

            // Filter by confidence threshold
            if (confidence < confidenceThreshold || maxClassIndex < 0)
                continue;

            // Convert from letterboxed coordinates (640x640) to original image coordinates
            // Step 1: Remove padding offset to get coordinates in scaled image space
            var centerXScaled = centerX - padX;
            var centerYScaled = centerY - padY;

            // Step 2: Scale back to original image dimensions
            var x = (centerXScaled - width / 2) / scale;
            var y = (centerYScaled - height / 2) / scale;
            var w = width / scale;
            var h = height / scale;

            // Clamp coordinates to image bounds to prevent negative dimensions
            var clampedX = Math.Max(0, Math.Min(x, originalWidth));
            var clampedY = Math.Max(0, Math.Min(y, originalHeight));

            // Calculate right and bottom edges, clamped to image bounds
            var rightEdge = Math.Min(x + w, originalWidth);
            var bottomEdge = Math.Min(y + h, originalHeight);

            // Calculate final width and height (guaranteed non-negative)
            var clampedW = Math.Max(0, rightEdge - clampedX);
            var clampedH = Math.Max(0, bottomEdge - clampedY);

            detections.Add(new DetectedObjectOnnx
            {
                ClassName = CocoClassNames.Names[maxClassIndex],
                Confidence = confidence,
                X = clampedX,
                Y = clampedY,
                Width = clampedW,
                Height = clampedH
            });
        }

        return detections;
    }

    /// <summary>
    /// Parse YOLOv8 output: [1, 84, 8400] channels-first layout
    /// All predictions for each channel are grouped together
    /// </summary>
    private static List<DetectedObjectOnnx> ParseYoloV8Output(float[] output, int originalWidth, int originalHeight, float scale, int padX, int padY, float confidenceThreshold)
    {
        var detections = new List<DetectedObjectOnnx>();

        // YOLOv8 output format: [1, 84, 8400]
        // Layout: channels-first (CHW), where:
        // - Channel dimension (84) = 4 bbox coords + 80 class scores (no objectness)
        // - Predictions dimension (8400) = number of anchor boxes
        // Memory layout: [all_centerX, all_centerY, all_width, all_height, all_class0_scores, ..., all_class79_scores]
        // To access prediction i for channel c: output[c * 8400 + i]

        for (int i = 0; i < YoloV8NumPredictions; i++)
        {
            // Read bbox coordinates using channel stride
            var centerX = output[0 * YoloV8NumPredictions + i];  // Channel 0
            var centerY = output[1 * YoloV8NumPredictions + i];  // Channel 1
            var width = output[2 * YoloV8NumPredictions + i];    // Channel 2
            var height = output[3 * YoloV8NumPredictions + i];   // Channel 3

            // Find max class score across 80 classes
            var maxClassScore = 0f;
            var maxClassIndex = -1;

            for (int classIdx = 0; classIdx < NumClasses; classIdx++)
            {
                var classScore = output[(4 + classIdx) * YoloV8NumPredictions + i];
                if (classScore > maxClassScore)
                {
                    maxClassScore = classScore;
                    maxClassIndex = classIdx;
                }
            }

            // Filter by confidence threshold (YOLOv8 has no objectness score)
            if (maxClassScore < confidenceThreshold || maxClassIndex < 0)
                continue;

            // Convert from letterboxed coordinates (640x640) to original image coordinates
            // Step 1: Remove padding offset to get coordinates in scaled image space
            var centerXScaled = centerX - padX;
            var centerYScaled = centerY - padY;

            // Step 2: Scale back to original image dimensions
            var x = (centerXScaled - width / 2) / scale;
            var y = (centerYScaled - height / 2) / scale;
            var w = width / scale;
            var h = height / scale;

            // Clamp coordinates to image bounds to prevent negative dimensions
            var clampedX = Math.Max(0, Math.Min(x, originalWidth));
            var clampedY = Math.Max(0, Math.Min(y, originalHeight));

            // Calculate right and bottom edges, clamped to image bounds
            var rightEdge = Math.Min(x + w, originalWidth);
            var bottomEdge = Math.Min(y + h, originalHeight);

            // Calculate final width and height (guaranteed non-negative)
            var clampedW = Math.Max(0, rightEdge - clampedX);
            var clampedH = Math.Max(0, bottomEdge - clampedY);

            detections.Add(new DetectedObjectOnnx
            {
                ClassName = CocoClassNames.Names[maxClassIndex],
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
    /// Applies class-aware Non-Maximum Suppression (NMS) to filter overlapping detections.
    /// NMS is applied independently for each class to avoid removing valid detections
    /// of different objects that happen to overlap (e.g., person on a bicycle).
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
                // Only compare within the same class
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

    // Dispose is inherited from OnnxInferenceServiceBase
}
