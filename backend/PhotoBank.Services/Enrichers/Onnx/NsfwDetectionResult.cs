using System.Collections.Generic;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// NSFW detection result
/// </summary>
public class NsfwDetectionResult
{
    public bool IsNsfw { get; set; }
    public float NsfwConfidence { get; set; }
    public bool IsRacy { get; set; }
    public float RacyConfidence { get; set; }
    public Dictionary<string, float> Scores { get; set; } = new();
}
