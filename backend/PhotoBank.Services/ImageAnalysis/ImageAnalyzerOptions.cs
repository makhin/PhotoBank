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

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string Endpoint { get; init; } = "https://openrouter.ai/api/v1/chat/completions";
    public string Model { get; init; } = "openai/gpt-4o-mini";
    public string ApiKey { get; init; } = string.Empty;
    public double Temperature { get; init; } = 0.3;
    public int MaxTokens { get; init; } = 1024;
}
