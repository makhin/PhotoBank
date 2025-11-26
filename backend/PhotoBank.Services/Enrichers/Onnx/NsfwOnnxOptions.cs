namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Configuration options for NSFW ONNX model
/// </summary>
public class NsfwOnnxOptions
{
    public bool Enabled { get; set; }
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Threshold for porn classification (default: 0.5)
    /// </summary>
    public float PornThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Threshold for sexy classification (default: 0.7)
    /// </summary>
    public float SexyThreshold { get; set; } = 0.7f;

    /// <summary>
    /// Threshold for hentai classification (default: 0.6)
    /// </summary>
    public float HentaiThreshold { get; set; } = 0.6f;

    /// <summary>
    /// Threshold for racy classification (min: 0.4, max: 0.7, default)
    /// </summary>
    public float RacyMinThreshold { get; set; } = 0.4f;
    public float RacyMaxThreshold { get; set; } = 0.7f;
}
