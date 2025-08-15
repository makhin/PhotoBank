namespace PhotoBank.Services.FaceRecognition.Azure;

public sealed class AzureFaceOptions
{
    public string Endpoint { get; init; } = "https://<your-face-endpoint>.cognitiveservices.azure.com/";
    public string Key { get; init; } = "<your-key>";
    public string PersonGroupId { get; init; } = "my-circle-person-group";
    public string RecognitionModel { get; init; } = "recognition_04"; // or recognition_03
    public string DetectionModel { get; init; } = "detection_01";     // detection_01 | detection_02
    public int IdentifyChunkSize { get; init; } = 10;
    public int TrainTimeoutSeconds { get; init; } = 300;
}
