namespace PhotoBank.Services.Translator;

public sealed class TranslatorOptions
{
    public string Endpoint { get; init; } = "https://api.cognitive.microsofttranslator.com";
    public string Region { get; init; } = "westeurope";
    public string Key { get; init; } = ""; // comes from environment
}

