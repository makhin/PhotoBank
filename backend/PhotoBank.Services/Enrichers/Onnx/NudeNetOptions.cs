namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Configuration options for NudeNet NSFW detection service
/// </summary>
public class NudeNetOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://localhost:5556";
}
