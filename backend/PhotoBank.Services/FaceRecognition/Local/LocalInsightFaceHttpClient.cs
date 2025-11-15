using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace PhotoBank.Services.FaceRecognition.Local;

public interface ILocalInsightFaceClient
{
    Task<LocalDetectResponse> DetectAsync(Stream image, CancellationToken ct);
    Task<LocalEmbedResponse> EmbedAsync(Stream image, bool includeAttributes, CancellationToken ct);
    Task<LocalAttributesResponse> AttributesAsync(Stream image, CancellationToken ct);
}

public sealed class LocalInsightFaceHttpClient : ILocalInsightFaceClient
{
    private readonly HttpClient _http;
    private readonly LocalInsightFaceOptions _opts;

    public LocalInsightFaceHttpClient(HttpClient http, IOptions<LocalInsightFaceOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
        _http.BaseAddress = new Uri(_opts.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// DEPRECATED: Face detection endpoint removed. Service now works only with pre-cropped faces.
    /// </summary>
    public Task<LocalDetectResponse> DetectAsync(Stream image, CancellationToken ct)
    {
        throw new NotSupportedException(
            "Face detection endpoint has been removed. " +
            "InsightFace service now works only with pre-cropped face images. " +
            "Use EmbedAsync or AttributesAsync with cropped face images instead.");
    }

    /// <summary>
    /// Extract face embedding from a pre-cropped face image using ArcFace (Glint360K) model
    /// </summary>
    /// <param name="image">Stream containing cropped face image (will be resized to 112x112)</param>
    /// <param name="includeAttributes">Whether to include face attributes (age, gender, pose) in response</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Face embedding response with 512-dimensional vector and optional attributes</returns>
    public async Task<LocalEmbedResponse> EmbedAsync(Stream image, bool includeAttributes, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var sc = new StreamContent(image);
        sc.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(sc, "file", "face.jpg");

        var endpoint = includeAttributes ? "/embed?include_attributes=true" : "/embed";
        var res = await _http.PostAsync(endpoint, form, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<LocalEmbedResponse>(json, JsonOpts())!;
    }

    /// <summary>
    /// Extract face attributes from a pre-cropped face image
    /// </summary>
    /// <param name="image">Stream containing cropped face image</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Face attributes including age, gender, and pose</returns>
    public async Task<LocalAttributesResponse> AttributesAsync(Stream image, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var sc = new StreamContent(image);
        sc.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(sc, "file", "face.jpg");

        var res = await _http.PostAsync("/attributes", form, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<LocalAttributesResponse>(json, JsonOpts())!;
    }

    private static JsonSerializerOptions JsonOpts() => new(JsonSerializerDefaults.Web)
    { PropertyNameCaseInsensitive = true };
}

public sealed record LocalDetectResponse(List<LocalDetectedFace> Faces);
public sealed record LocalDetectedFace(string Id, float Score, float[]? Bbox, float[]? Landmark, float? Age, string? Gender);

/// <summary>
/// Response from InsightFace /embed endpoint
/// Contains face embedding vector from ArcFace (Glint360K) model
/// </summary>
public sealed record LocalEmbedResponse(
    float[] Embedding,
    int[]? EmbeddingShape,
    int? EmbeddingDim,
    string? Model,
    string? InputSize,
    FaceAttributes? Attributes
);

/// <summary>
/// Response from InsightFace /attributes endpoint
/// </summary>
public sealed record LocalAttributesResponse(
    string Status,
    FaceAttributes Attributes
);

/// <summary>
/// Face attributes extracted from a face image
/// </summary>
public sealed record FaceAttributes(
    int? Age,
    string? Gender,
    PoseInfo? Pose,
    bool? EmbeddingAvailable,
    int? EmbeddingDim
);

/// <summary>
/// Head pose angles (in degrees)
/// </summary>
public sealed record PoseInfo(
    float? Yaw,
    float? Pitch,
    float? Roll
);
