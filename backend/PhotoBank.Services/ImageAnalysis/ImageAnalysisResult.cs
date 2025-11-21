using System.Collections.Generic;

namespace PhotoBank.Services.ImageAnalysis;

/// <summary>
/// Provider-agnostic image analysis result.
/// Supports Azure Cognitive Services and Ollama vision models.
/// </summary>
public sealed class ImageAnalysisResult
{
    public ImageDescription? Description { get; init; }
    public IReadOnlyList<ImageTag> Tags { get; init; } = [];
    public IReadOnlyList<ImageCategory> Categories { get; init; } = [];
    public IReadOnlyList<DetectedObject> Objects { get; init; } = [];
    public AdultContent? Adult { get; init; }
    public ColorInfo? Color { get; init; }
}

public sealed class ImageDescription
{
    public IReadOnlyList<ImageCaption> Captions { get; init; } = [];
}

public sealed class ImageCaption
{
    public required string Text { get; init; }
    public double Confidence { get; init; }
}

public sealed class ImageTag
{
    public required string Name { get; init; }
    public double Confidence { get; init; }
}

public sealed class ImageCategory
{
    public required string Name { get; init; }
    public double Score { get; init; }
}

public sealed class DetectedObject
{
    public required string ObjectProperty { get; init; }
    public double Confidence { get; init; }
    public ObjectRectangle? Rectangle { get; init; }
}

public sealed class ObjectRectangle
{
    public int X { get; init; }
    public int Y { get; init; }
    public int W { get; init; }
    public int H { get; init; }
}

public sealed class AdultContent
{
    public bool IsAdultContent { get; init; }
    public double AdultScore { get; init; }
    public bool IsRacyContent { get; init; }
    public double RacyScore { get; init; }
}

public sealed class ColorInfo
{
    public bool IsBWImg { get; init; }
    public string? AccentColor { get; init; }
    public string? DominantColorBackground { get; init; }
    public string? DominantColorForeground { get; init; }
    public IReadOnlyList<string> DominantColors { get; init; } = [];
}
