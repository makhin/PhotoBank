namespace PhotoBank.Services.FaceRecognition.Local;

public sealed class LocalInsightFaceOptions
{
    public string BaseUrl { get; init; } = "http://localhost:18081";
    public int MaxParallelism { get; init; } = 6;
    public string Model { get; init; } = "buffalo_l";
    public float FaceMatchThreshold { get; init; } = 0.45f; // косинус
    public int TopK { get; init; } = 10;

    /// <summary>
    /// Number of top candidates to return per face in IdentifyAsync
    /// </summary>
    public int IdentifyMaxCandidates { get; init; } = 5;

    /// <summary>
    /// Minimum confidence threshold for automatic identification (0-1, higher = stricter)
    /// Default 0.6 means 60% similarity required
    /// </summary>
    public float AutoIdentifyThreshold { get; init; } = 0.6f;
}
