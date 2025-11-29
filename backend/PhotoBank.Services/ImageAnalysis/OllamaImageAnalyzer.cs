using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace PhotoBank.Services.ImageAnalysis;

public sealed class OllamaImageAnalyzer : IImageAnalyzer
{
    private readonly OllamaApiClient _client;
    private readonly ILogger<OllamaImageAnalyzer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };


    public OllamaImageAnalyzer(IOptions<OllamaOptions> options, ILogger<OllamaImageAnalyzer> logger)
    {
        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client = new OllamaApiClient(new Uri(opts.Endpoint))
        {
            SelectedModel = opts.Model
        };
    }

    public ImageAnalyzerKind Kind => ImageAnalyzerKind.Ollama;

    public async Task<ImageAnalysisResult> AnalyzeAsync(Stream image, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct).ConfigureAwait(false);
        var imageBytes = ms.ToArray();

        var jsonResponse = await RetryHelper.RetryAsync(
            action: async () =>
            {
                var chat = new Chat(_client);
                var images = new List<IEnumerable<byte>> { imageBytes };
                var responseBuilder = new StringBuilder();

                await foreach (var token in chat.SendAsync(ImageAnalysisPrompts.StandardAnalysisPrompt, imagesAsBytes: images, ct))
                {
                    responseBuilder.Append(token);
                }

                return responseBuilder.ToString();
            },
            attempts: 3,
            delay: TimeSpan.FromMilliseconds(500),
            shouldRetry: ex => ex is HttpRequestException or SocketException or TaskCanceledException { InnerException: TimeoutException }
        ).ConfigureAwait(false);

        _logger.LogDebug("Ollama response: {Response}", jsonResponse);

        return ParseResponse(jsonResponse);
    }

    internal ImageAnalysisResult ParseResponse(string json)
    {
        try
        {
            json = CleanJsonResponse(json);
            var response = JsonSerializer.Deserialize<OllamaResponse>(json, JsonOptions);

            if (response is null)
            {
                return CreateEmptyResult();
            }

            var tags = response.Tags?
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => new ImageTag { Name = t.Name!, Confidence = t.Confidence })
                .ToList() ?? [];

            var dominantColors = response.DominantColors?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => char.ToUpperInvariant(c[0]) + c[1..].ToLowerInvariant())
                .ToList() ?? [];

            return new ImageAnalysisResult
            {
                Description = !string.IsNullOrWhiteSpace(response.Caption)
                    ? new ImageDescription
                    {
                        Captions = [new ImageCaption { Text = response.Caption, Confidence = 0.9 }]
                    }
                    : null,

                Tags = tags,
                Categories = [],
                Objects = [],

                Adult = new AdultContent
                {
                    IsAdultContent = response.IsNsfw,
                    AdultScore = response.IsNsfw ? 0.9 : 0.1,
                    IsRacyContent = response.IsRacy,
                    RacyScore = response.IsRacy ? 0.9 : 0.1
                },

                Color = new ColorInfo
                {
                    IsBWImg = false,
                    AccentColor = dominantColors.FirstOrDefault(),
                    DominantColorBackground = dominantColors.FirstOrDefault(),
                    DominantColorForeground = dominantColors.Skip(1).FirstOrDefault() ?? dominantColors.FirstOrDefault(),
                    DominantColors = dominantColors
                }
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Ollama response: {Response}", json);
            // Fail closed: rethrow to prevent NSFW content from bypassing checks
            throw new InvalidOperationException($"Ollama returned malformed response that could not be parsed", ex);
        }
    }

    private static string CleanJsonResponse(string json)
    {
        json = json.Trim();
        if (json.StartsWith("```"))
        {
            var startIdx = json.IndexOf('{');
            var endIdx = json.LastIndexOf('}');
            if (startIdx >= 0 && endIdx > startIdx)
            {
                json = json.Substring(startIdx, endIdx - startIdx + 1);
            }
        }
        return json;
    }

    private static ImageAnalysisResult CreateEmptyResult() => new()
    {
        Description = new ImageDescription { Captions = [] },
        Tags = [],
        Categories = [],
        Objects = [],
        Adult = new AdultContent(),
        Color = new ColorInfo()
    };

    private sealed class OllamaResponse
    {
        [JsonPropertyName("caption")]
        public string? Caption { get; init; }

        [JsonPropertyName("tags")]
        public List<OllamaTag>? Tags { get; init; }

        [JsonPropertyName("is_nsfw")]
        public bool IsNsfw { get; init; }

        [JsonPropertyName("is_racy")]
        public bool IsRacy { get; init; }

        [JsonPropertyName("dominant_colors")]
        public List<string>? DominantColors { get; init; }
    }

    private sealed class OllamaTag
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; } = 0.5;
    }
}
