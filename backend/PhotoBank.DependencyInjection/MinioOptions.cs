namespace PhotoBank.DependencyInjection;

public sealed class MinioOptions
{
    public string Endpoint { get; init; } = "localhost";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}

