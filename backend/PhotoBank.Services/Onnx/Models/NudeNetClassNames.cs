namespace PhotoBank.Services.Onnx.Models;

/// <summary>
/// NudeNet class names (18 classes for nudity/body part detection)
/// Based on NudeNet v3.4+ ONNX models
/// </summary>
public static class NudeNetClassNames
{
    public static readonly string[] Names = new[]
    {
        "FEMALE_GENITALIA_COVERED",
        "FACE_FEMALE",
        "BUTTOCKS_EXPOSED",
        "FEMALE_BREAST_EXPOSED",
        "FEMALE_GENITALIA_EXPOSED",
        "MALE_BREAST_EXPOSED",
        "ANUS_EXPOSED",
        "FEET_EXPOSED",
        "BELLY_COVERED",
        "FEET_COVERED",
        "ARMPITS_COVERED",
        "ARMPITS_EXPOSED",
        "FACE_MALE",
        "BELLY_EXPOSED",
        "MALE_GENITALIA_EXPOSED",
        "ANUS_COVERED",
        "FEMALE_BREAST_COVERED",
        "BUTTOCKS_COVERED"
    };

    /// <summary>
    /// Classes that indicate explicit NSFW content
    /// </summary>
    public static readonly string[] ExplicitClasses = new[]
    {
        "FEMALE_GENITALIA_EXPOSED",
        "MALE_GENITALIA_EXPOSED",
        "FEMALE_BREAST_EXPOSED",
        "ANUS_EXPOSED"
    };

    /// <summary>
    /// Classes that indicate racy/suggestive content
    /// </summary>
    public static readonly string[] RacyClasses = new[]
    {
        "BUTTOCKS_EXPOSED",
        "BELLY_EXPOSED",
        "ARMPITS_EXPOSED",
        "MALE_BREAST_EXPOSED"
    };
}
