namespace PhotoBank.Services.FaceRecognition.Local;

public sealed class LocalInsightFaceOptions
{
    public string BaseUrl { get; init; } = "http://localhost:5555";
    public int MaxParallelism { get; init; } = 6;
    public string Model { get; init; } = "buffalo_l";
    public float FaceMatchThreshold { get; init; } = 0.45f; // косинус
    public int TopK { get; init; } = 10;
}
