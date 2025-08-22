namespace PhotoBank.Services.Translator;

public sealed record TranslateRequest(
    string[] Texts,
    string To,
    string? From = null,
    string TextType = "plain",
    string? ProfanityAction = null,
    string? Category = null
);

