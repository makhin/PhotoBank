namespace PhotoBank.Services.FaceRecognition.Aws;

public sealed class RekognitionOptions
{
    public string CollectionId { get; init; } = "my-circle-person-group";
    public int MaxParallelism { get; init; } = 6;
    public float FaceMatchThreshold { get; init; } = 90f; // Rekognition similarity threshold (0..100)
    public string QualityFilter { get; init; } = "AUTO";  // NONE | AUTO | LOW | MEDIUM | HIGH
}
