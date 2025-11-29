using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagick;
using Microsoft.Extensions.Options;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;
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

    public IReadOnlyList<DetectedObjectDto> DetectObjects(SourceDataDto sourceData, float scale)
    {
        if (sourceData?.LetterboxedImage640 == null)
            return Array.Empty<DetectedObjectDto>();

        if (sourceData.OriginalImage == null)
            return Array.Empty<DetectedObjectDto>();

        // Use pre-prepared letterboxed image and parameters from PreviewEnricher
        var detectedObjects = _yoloService.DetectObjects(
            letterboxedImage: sourceData.LetterboxedImage640,
            originalWidth: (int)sourceData.OriginalImage.Width,
            originalHeight: (int)sourceData.OriginalImage.Height,
            letterboxScale: sourceData.LetterboxScale,
            padX: sourceData.LetterboxPadX,
            padY: sourceData.LetterboxPadY,
            confidenceThreshold: _confidenceThreshold,
            nmsThreshold: _nmsThreshold);

        if (detectedObjects.Count == 0)
            return Array.Empty<DetectedObjectDto>();

        // Convert YOLO detections to common format
        // Note: YOLO already returns coordinates in original image space,
        // so we scale them by photo scale factor for database storage
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
