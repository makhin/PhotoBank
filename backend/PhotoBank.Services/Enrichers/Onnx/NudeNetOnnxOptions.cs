namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Configuration options for NudeNet ONNX model
/// </summary>
public class NudeNetOnnxOptions
{
    public bool Enabled { get; set; }
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Model input resolution (320 for 320n.onnx, 640 for 640m.onnx)
    /// Default: 320 (faster, sufficient accuracy)
    /// </summary>
    public int InputResolution { get; set; } = 320;

    /// <summary>
    /// Minimum confidence threshold for detections (default: 0.5)
    /// Lower values = more sensitive detection
    /// </summary>
    public float ConfidenceThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Non-Maximum Suppression threshold (default: 0.45)
    /// Controls filtering of overlapping detections
    /// </summary>
    public float NmsThreshold { get; set; } = 0.45f;

    /// <summary>
    /// Minimum confidence for explicit NSFW classification (default: 0.6)
    /// Applies to: FEMALE_GENITALIA_EXPOSED, MALE_GENITALIA_EXPOSED, FEMALE_BREAST_EXPOSED, ANUS_EXPOSED
    /// </summary>
    public float ExplicitThreshold { get; set; } = 0.6f;

    /// <summary>
    /// Minimum confidence for racy content classification (default: 0.5)
    /// Applies to: BUTTOCKS_EXPOSED, BELLY_EXPOSED, ARMPITS_EXPOSED, MALE_BREAST_EXPOSED
    /// </summary>
    public float RacyThreshold { get; set; } = 0.5f;
}
