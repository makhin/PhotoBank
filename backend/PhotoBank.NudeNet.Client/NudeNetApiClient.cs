using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.NudeNetApiClient;

public interface INudeNetApiClient : IDisposable
{
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
    Task<NudeNetDetectionResult> DetectAsync(Stream imageStream, string fileName = "image.jpg", CancellationToken cancellationToken = default);
}

/// <summary>
/// Detection result from NudeNet API
/// </summary>
public class NudeNetDetectionResult
{
    [JsonPropertyName("is_nsfw")]
    public bool IsNsfw { get; set; }

    [JsonPropertyName("nsfw_confidence")]
    public float NsfwConfidence { get; set; }

    [JsonPropertyName("is_racy")]
    public bool IsRacy { get; set; }

    [JsonPropertyName("racy_confidence")]
    public float RacyConfidence { get; set; }

    [JsonPropertyName("scores")]
    public Dictionary<string, float> Scores { get; set; } = new();

    [JsonPropertyName("detections")]
    public List<NudeNetDetection> Detections { get; set; } = new();
}

/// <summary>
/// Individual detection from NudeNet
/// </summary>
public class NudeNetDetection
{
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("box")]
    public List<float> Box { get; set; } = new();
}

/// <summary>
/// HTTP client for NudeNet NSFW detection API
/// </summary>
public class NudeNetApiClient : INudeNetApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public NudeNetApiClient(string baseUrl = "http://localhost:5556")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public NudeNetApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/health", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<NudeNetDetectionResult> DetectAsync(Stream imageStream, string fileName = "image.jpg", CancellationToken cancellationToken = default)
    {
        if (imageStream == null)
            throw new ArgumentNullException(nameof(imageStream));

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("/detect", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<NudeNetDetectionResult>(responseJson, _jsonOptions);

        return result ?? throw new InvalidOperationException("Failed to deserialize NudeNet response");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
