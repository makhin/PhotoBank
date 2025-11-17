using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Service for YOLO ONNX model inference
/// </summary>
public interface IYoloOnnxService : IDisposable
{
    List<DetectedObjectOnnx> DetectObjects(byte[] imageData, float confidenceThreshold = 0.5f, float nmsThreshold = 0.45f);
}

public class YoloOnnxService : IYoloOnnxService
{
    private readonly MLContext _mlContext;
    private readonly PredictionEngine<YoloImageInput, YoloOutput> _predictionEngine;
    private readonly string _modelPath;
    private const int InputWidth = 640;
    private const int InputHeight = 640;
    private const int OutputDimensions = 84; // 4 bbox coords + 80 classes for YOLOv8

    public YoloOnnxService(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"ONNX model file not found at: {modelPath}", modelPath);

        _modelPath = modelPath;
        _mlContext = new MLContext();

        // Load the ONNX model
        var pipeline = _mlContext.Transforms.ApplyOnnxModel(
            modelFile: _modelPath,
            outputColumnNames: new[] { "output0" },
            inputColumnNames: new[] { "images" }
        );

        var emptyData = _mlContext.Data.LoadFromEnumerable(new List<YoloImageInput>());
        var model = pipeline.Fit(emptyData);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<YoloImageInput, YoloOutput>(model);
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

        // Run prediction
        var output = _predictionEngine.Predict(input);

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

        // YOLOv8 output format: [batch, 84, 8400]
        // 84 = 4 bbox coordinates + 80 class scores
        // 8400 = number of predictions

        var numPredictions = output.Length / OutputDimensions;

        for (int i = 0; i < numPredictions; i++)
        {
            var offset = i * OutputDimensions;

            // Get bbox coordinates (center_x, center_y, width, height)
            var centerX = output[offset];
            var centerY = output[offset + 1];
            var width = output[offset + 2];
            var height = output[offset + 3];

            // Get class scores (80 classes)
            var maxClassScore = 0f;
            var maxClassIndex = -1;

            for (int classIdx = 0; classIdx < 80; classIdx++)
            {
                var classScore = output[offset + 4 + classIdx];
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

    private List<DetectedObjectOnnx> ApplyNMS(List<DetectedObjectOnnx> detections, float nmsThreshold)
    {
        var result = new List<DetectedObjectOnnx>();
        var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();

        while (sortedDetections.Count > 0)
        {
            var best = sortedDetections[0];
            result.Add(best);
            sortedDetections.RemoveAt(0);

            sortedDetections = sortedDetections
                .Where(d => CalculateIoU(best, d) < nmsThreshold)
                .ToList();
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

    public void Dispose()
    {
        _predictionEngine?.Dispose();
    }
}
