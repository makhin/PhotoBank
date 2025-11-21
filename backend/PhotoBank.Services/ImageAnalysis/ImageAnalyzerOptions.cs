namespace PhotoBank.Services.ImageAnalysis;

public sealed class ImageAnalyzerOptions
{
    public const string SectionName = "ImageAnalyzer";

    public ImageAnalyzerKind Provider { get; init; } = ImageAnalyzerKind.Azure;
}

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "qwen2.5vl";
}
