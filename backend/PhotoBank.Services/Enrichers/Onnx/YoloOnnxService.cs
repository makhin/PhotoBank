using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.ML;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Service for YOLO ONNX model inference (thread-safe)
/// </summary>
public interface IYoloOnnxService
{
    List<DetectedObjectOnnx> DetectObjects(byte[] imageData, float confidenceThreshold = 0.5f, float nmsThreshold = 0.45f);
}

/// <summary>
/// Thread-safe YOLO service using PredictionEnginePool
/// </summary>
public class YoloOnnxService : IYoloOnnxService
{
    private readonly PredictionEnginePool<YoloImageInput, YoloOutput> _predictionEnginePool;
    private const int InputWidth = 640;
    private const int InputHeight = 640;
    private const int NumClasses = 80;
    private const int NumPredictions = 8400; // YOLOv8 outputs 8400 predictions

    public YoloOnnxService(PredictionEnginePool<YoloImageInput, YoloOutput> predictionEnginePool)
    {
        _predictionEnginePool = predictionEnginePool ?? throw new ArgumentNullException(nameof(predictionEnginePool));
    }

    public List<DetectedObjectOnnx> DetectObjects(byte[] imageData, float confidenceThreshold = 0.5f, float nmsThreshold = 0.45f)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

        using var image = Image.Load<Rgb24>(imageData);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Prepare input
        var input = PrepareInput(image);

        // Run prediction using thread-safe pool
        var output = _predictionEnginePool.Predict(input);

        if (output?.Output == null || output.Output.Length == 0)
            return new List<DetectedObjectOnnx>();

        // Parse output and apply NMS
        var detections = ParseOutput(output.Output, originalWidth, originalHeight, confidenceThreshold);
        var filteredDetections = ApplyNMS(detections, nmsThreshold);

        return filteredDetections;
    }

    private YoloImageInput PrepareInput(Image<Rgb24> image)
    {
        // Resize image to 640x640
        image.Mutate(x => x.Resize(InputWidth, InputHeight));

        var input = new float[1 * 3 * InputHeight * InputWidth];
        var index = 0;

        // Convert to CHW format (Channel, Height, Width) and normalize [0, 255] -> [0, 1]
        for (int c = 0; c < 3; c++) // RGB channels
        {
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var pixel = image[x, y];
                    input[index++] = c switch
                    {
                        0 => pixel.R / 255f,
                        1 => pixel.G / 255f,
                        2 => pixel.B / 255f,
                        _ => 0
                    };
                }
            }
        }

        return new YoloImageInput { Image = input };
    }

    private List<DetectedObjectOnnx> ParseOutput(float[] output, int originalWidth, int originalHeight, float confidenceThreshold)
    {
        var detections = new List<DetectedObjectOnnx>();

        // YOLOv8 output format: [1, 84, 8400]
        // Layout: channels-first (CHW), where:
        // - Channel dimension (84) = 4 bbox coords + 80 class scores
        // - Predictions dimension (8400) = number of anchor boxes
        // Memory layout: [all_centerX, all_centerY, all_width, all_height, all_class0_scores, ..., all_class79_scores]
        // To access prediction i for channel c: output[c * NumPredictions + i]

        for (int i = 0; i < NumPredictions; i++)
        {
            // Read bbox coordinates using channel stride
            var centerX = output[0 * NumPredictions + i];  // Channel 0
            var centerY = output[1 * NumPredictions + i];  // Channel 1
            var width = output[2 * NumPredictions + i];    // Channel 2
            var height = output[3 * NumPredictions + i];   // Channel 3

            // Find max class score across 80 classes
            var maxClassScore = 0f;
            var maxClassIndex = -1;

            for (int classIdx = 0; classIdx < NumClasses; classIdx++)
            {
                var classScore = output[(4 + classIdx) * NumPredictions + i];
                if (classScore > maxClassScore)
                {
                    maxClassScore = classScore;
                    maxClassIndex = classIdx;
                }
            }

            // Filter by confidence threshold
            if (maxClassScore < confidenceThreshold || maxClassIndex < 0)
                continue;

            // Convert from center coordinates to corner coordinates
            // and scale to original image size
            var x = (centerX - width / 2) * originalWidth / InputWidth;
            var y = (centerY - height / 2) * originalHeight / InputHeight;
            var w = width * originalWidth / InputWidth;
            var h = height * originalHeight / InputHeight;

            detections.Add(new DetectedObjectOnnx
            {
                ClassName = CocoClassNames.Names[maxClassIndex],
                Confidence = maxClassScore,
                X = Math.Max(0, x),
                Y = Math.Max(0, y),
                Width = Math.Min(w, originalWidth - x),
                Height = Math.Min(h, originalHeight - y)
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

    private float CalculateIoU(DetectedObjectOnnx box1, DetectedObjectOnnx box2)
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
}
