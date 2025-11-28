using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagick;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.Services.ObjectDetection.Abstractions;

namespace PhotoBank.Services.ObjectDetection;

/// <summary>
/// Object detection provider using Azure Computer Vision ImageAnalysis.
/// Extracts objects from ImageAnalysisResult that already contains detected objects.
/// </summary>
public class AzureObjectDetectionProvider : IObjectDetectionProvider
{
    public ObjectDetectionProviderKind Kind => ObjectDetectionProviderKind.Azure;

    /// <summary>
    /// Extracts detected objects from Azure Computer Vision ImageAnalysisResult.
    /// Note: This provider expects ImageAnalysisResult to be already populated in SourceDataDto
    /// by AnalyzeEnricher, so this method signature is for interface compatibility.
    /// Use GetDetectedObjectsFromAnalysis() to extract objects from existing analysis result.
    /// </summary>
    public IReadOnlyList<DetectedObjectDto> DetectObjects(IMagickImage<byte> image, float scale)
    {
        throw new InvalidOperationException(
            "AzureObjectDetectionProvider does not detect objects directly from image. " +
            "Use GetDetectedObjectsFromAnalysis() with ImageAnalysisResult from SourceDataDto.");
    }

    /// <summary>
    /// Extracts detected objects from Azure Computer Vision ImageAnalysisResult.
    /// </summary>
    /// <param name="analysisResult">Image analysis result from Azure Computer Vision.</param>
    /// <param name="scale">Photo scale factor for coordinate normalization.</param>
    /// <returns>List of detected objects with bounding boxes.</returns>
    public IReadOnlyList<DetectedObjectDto> GetDetectedObjectsFromAnalysis(
        ImageAnalysisResult? analysisResult,
        float scale)
    {
        if (analysisResult?.Objects == null)
            return Array.Empty<DetectedObjectDto>();

        var detectedObjects = new List<DetectedObjectDto>();

        foreach (var obj in analysisResult.Objects)
        {
            // Skip objects without rectangles (EF model requires Rectangle)
            if (obj.Rectangle == null)
                continue;

            var detectedObject = new DetectedObjectDto(
                ClassName: obj.ObjectProperty,
                Confidence: (float)obj.Confidence,
                X: (int)(obj.Rectangle.X / scale),
                Y: (int)(obj.Rectangle.Y / scale),
                Width: (int)(obj.Rectangle.W / scale),
                Height: (int)(obj.Rectangle.H / scale)
            );

            detectedObjects.Add(detectedObject);
        }

        return detectedObjects;
    }
}
