using System.Collections.Generic;
using ImageMagick;

namespace PhotoBank.Services.ObjectDetection.Abstractions;

/// <summary>
/// Provider kind for object detection.
/// </summary>
public enum ObjectDetectionProviderKind
{
    Azure,
    YoloOnnx
}

/// <summary>
/// Detected object with bounding box and confidence.
/// </summary>
/// <param name="ClassName">Name of the detected object class.</param>
/// <param name="Confidence">Detection confidence (0.0 to 1.0).</param>
/// <param name="X">X coordinate of bounding box.</param>
/// <param name="Y">Y coordinate of bounding box.</param>
/// <param name="Width">Width of bounding box.</param>
/// <param name="Height">Height of bounding box.</param>
public sealed record DetectedObjectDto(
    string ClassName,
    float Confidence,
    int X,
    int Y,
    int Width,
    int Height
);

/// <summary>
/// Abstraction for object detection providers (Azure Computer Vision, YOLO ONNX, etc.).
/// </summary>
public interface IObjectDetectionProvider
{
    /// <summary>
    /// Gets the kind of object detection provider being used.
    /// </summary>
    ObjectDetectionProviderKind Kind { get; }

    /// <summary>
    /// Detects objects in the provided image.
    /// </summary>
    /// <param name="image">ImageMagick image to analyze.</param>
    /// <param name="scale">Photo scale factor for coordinate normalization.</param>
    /// <returns>List of detected objects with bounding boxes and confidence scores.</returns>
    IReadOnlyList<DetectedObjectDto> DetectObjects(IMagickImage<byte> image, float scale);
}
