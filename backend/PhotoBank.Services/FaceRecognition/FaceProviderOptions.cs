namespace PhotoBank.Services.FaceRecognition;

public sealed class FaceProviderOptions
{
    public Abstractions.FaceProviderKind Default { get; init; } = Abstractions.FaceProviderKind.Local;
}
