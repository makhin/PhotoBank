using System.Collections.Generic;
using PhotoBank.Services.Onnx.Models;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// NudeNet detection result with detailed body part detections
/// </summary>
public class NudeNetDetectionResult
{
    /// <summary>
    /// Whether the image contains explicit NSFW content
    /// (exposed genitalia, breasts, anus)
    /// </summary>
    public bool IsNsfw { get; set; }

    /// <summary>
    /// Confidence score for NSFW classification (0-1)
    /// Maximum confidence among explicit detections
    /// </summary>
    public float NsfwConfidence { get; set; }

    /// <summary>
    /// Whether the image contains racy/suggestive content
    /// (exposed buttocks, belly, armpits without explicit nudity)
    /// </summary>
    public bool IsRacy { get; set; }

    /// <summary>
    /// Confidence score for racy classification (0-1)
    /// Maximum confidence among racy detections
    /// </summary>
    public float RacyConfidence { get; set; }

    /// <summary>
    /// All detected objects with bounding boxes and confidences
    /// </summary>
    public List<DetectedObjectOnnx> Detections { get; set; } = new();

    /// <summary>
    /// Summary of detection counts by category
    /// </summary>
    public Dictionary<string, int> DetectionCounts { get; set; } = new();
}
