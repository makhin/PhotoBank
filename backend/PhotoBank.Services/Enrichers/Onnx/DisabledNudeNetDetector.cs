using System.Collections.Generic;
using ImageMagick;
using PhotoBank.Services.Onnx.Models;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Disabled NudeNet detector that returns empty results
/// Used when NudeNet is disabled or fails to initialize
/// </summary>
public class DisabledNudeNetDetector : INudeNetDetector
{
    public NudeNetDetectionResult Detect(
        IMagickImage<byte> letterboxedImage,
        int originalWidth,
        int originalHeight,
        float letterboxScale,
        int padX,
        int padY)
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

    public void Dispose()
    {
        // Nothing to dispose
    }
}
