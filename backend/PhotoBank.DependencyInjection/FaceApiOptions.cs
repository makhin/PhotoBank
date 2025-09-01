namespace PhotoBank.DependencyInjection;

public sealed class FaceApiOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
}
