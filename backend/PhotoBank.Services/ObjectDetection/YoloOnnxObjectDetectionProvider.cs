using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.ObjectDetection.Abstractions;

namespace PhotoBank.Services.ObjectDetection;

/// <summary>
/// Object detection provider using YOLO ONNX model.
/// </summary>
public class YoloOnnxObjectDetectionProvider : IObjectDetectionProvider
{
    private readonly IYoloOnnxService _yoloService;
    private readonly float _confidenceThreshold;
    private readonly float _nmsThreshold;

    public YoloOnnxObjectDetectionProvider(
        IYoloOnnxService yoloService,
        IOptions<YoloOnnxOptions> options)
    {
        _yoloService = yoloService ?? throw new ArgumentNullException(nameof(yoloService));

        if (options == null) throw new ArgumentNullException(nameof(options));

        _confidenceThreshold = options.Value.ConfidenceThreshold;
        _nmsThreshold = options.Value.NmsThreshold;
    }

    public ObjectDetectionProviderKind Kind => ObjectDetectionProviderKind.YoloOnnx;

    public IReadOnlyList<DetectedObjectDto> DetectObjects(byte[]? imageBytes, float scale)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return Array.Empty<DetectedObjectDto>();

        var detectedObjects = _yoloService.DetectObjects(imageBytes, _confidenceThreshold, _nmsThreshold);

        if (detectedObjects.Count == 0)
            return Array.Empty<DetectedObjectDto>();

        // Convert YOLO detections to common format
        return detectedObjects.Select(obj => new DetectedObjectDto(
            ClassName: obj.ClassName,
            Confidence: obj.Confidence,
            X: (int)(obj.X / scale),
            Y: (int)(obj.Y / scale),
            Width: (int)(obj.Width / scale),
            Height: (int)(obj.Height / scale)
        )).ToList();
    }
}
