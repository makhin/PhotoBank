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
    Task<LocalEmbedResponse> EmbedAsync(Stream image, CancellationToken ct);
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

    public async Task<LocalDetectResponse> DetectAsync(Stream image, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var sc = new StreamContent(image);
        sc.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(sc, "image", "image.jpg");

        var res = await _http.PostAsync("/detect", form, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<LocalDetectResponse>(json, JsonOpts())!;
    }

    public async Task<LocalEmbedResponse> EmbedAsync(Stream image, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var sc = new StreamContent(image);
        sc.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(sc, "image", "image.jpg");

        var res = await _http.PostAsync("/embed", form, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<LocalEmbedResponse>(json, JsonOpts())!;
    }

    private static JsonSerializerOptions JsonOpts() => new(JsonSerializerDefaults.Web)
    { PropertyNameCaseInsensitive = true };
}

public sealed record LocalDetectResponse(List<LocalDetectedFace> Faces);
public sealed record LocalDetectedFace(string Id, float Score, float[]? Bbox, float[]? Landmark, float? Age, string? Gender);
public sealed record LocalEmbedResponse(float[] Embedding, string? Model);
