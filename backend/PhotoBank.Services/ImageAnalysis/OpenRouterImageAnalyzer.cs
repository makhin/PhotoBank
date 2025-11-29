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

namespace PhotoBank.Services.ImageAnalysis;

public sealed class OpenRouterImageAnalyzer : IImageAnalyzer
{
    private readonly HttpClient _client;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterImageAnalyzer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private const string AnalysisPrompt = """
        You are an image analysis service used inside a photo management system.

        Your job:
        - Analyze the image.
        - Generate:
          1) A concise caption in English.
          2) A list of 5–15 high-level tags (single words or short phrases) that best describe objects, scenes, activities and attributes in the image. Each tag should have confidence level 0..1.
          3.1) Is nsfw: true/false.
          3.2) Is racy: true/false.
          4) Two or three dominant colors in the image.
        - Focus on the most prominent and relevant aspects of the image.

        Return a single JSON object:
        {
          "caption": "string",
          "tags": [{"name": "tag1", "confidence": 0.9}, {"name": "tag2", "confidence": 0.8}],
          "is_nsfw": false,
          "is_racy": false,
          "dominant_colors": ["color1", "color2"]
        }

        Rules:
        - Only JSON, no markdown, no comments.
        - Tags must be lowercase English.
        - Do not include duplicates (plural, singular).
        - Do not invent tags that are not visually grounded in the image.
        """;

    public OpenRouterImageAnalyzer(IOptions<OpenRouterOptions> options, ILogger<OpenRouterImageAnalyzer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _client.DefaultRequestHeaders.Add("HTTP-Referer", "https://photobank.app");
    }

    public ImageAnalyzerKind Kind => ImageAnalyzerKind.OpenRouter;

    public async Task<ImageAnalysisResult> AnalyzeAsync(Stream image, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct).ConfigureAwait(false);
        var imageBytes = ms.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var jsonResponse = await RetryHelper.RetryAsync(
            action: async () =>
            {
                var requestBody = new
                {
                    model = _options.Model,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = $"data:image/jpeg;base64,{base64Image}"
                                    }
                                },
                                new
                                {
                                    type = "text",
                                    text = AnalysisPrompt
                                }
                            }
                        }
                    },
                    max_tokens = _options.MaxTokens,
                    temperature = _options.Temperature
                };

                var json = JsonSerializer.Serialize(requestBody, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(_options.Endpoint, content, ct).ConfigureAwait(false);
                var responseText = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"OpenRouter API Error: {response.StatusCode} - {responseText}");
                }

                var apiResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseText, JsonOptions);
                return apiResponse?.Choices?[0]?.Message?.Content ?? string.Empty;
            },
            attempts: 3,
            delay: TimeSpan.FromMilliseconds(500),
            shouldRetry: ex => ex is HttpRequestException or SocketException or TaskCanceledException { InnerException: TimeoutException }
        ).ConfigureAwait(false);

        _logger.LogDebug("OpenRouter response: {Response}", jsonResponse);

        return ParseResponse(jsonResponse);
    }

    internal ImageAnalysisResult ParseResponse(string json)
    {
        try
        {
            json = CleanJsonResponse(json);
            var response = JsonSerializer.Deserialize<OpenRouterAnalysisResponse>(json, JsonOptions);

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
            _logger.LogError(ex, "Failed to deserialize OpenRouter response: {Response}", json);
            // Fail closed: rethrow to prevent NSFW content from bypassing checks
            throw new InvalidOperationException($"OpenRouter returned malformed response that could not be parsed", ex);
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

    private sealed class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; init; }

        public sealed class Choice
        {
            [JsonPropertyName("message")]
            public Message? Message { get; init; }
        }

        public sealed class Message
        {
            [JsonPropertyName("content")]
            public string? Content { get; init; }
        }
    }

    private sealed class OpenRouterAnalysisResponse
    {
        [JsonPropertyName("caption")]
        public string? Caption { get; init; }

        [JsonPropertyName("tags")]
        public List<OpenRouterTag>? Tags { get; init; }

        [JsonPropertyName("is_nsfw")]
        public bool IsNsfw { get; init; }

        [JsonPropertyName("is_racy")]
        public bool IsRacy { get; init; }

        [JsonPropertyName("dominant_colors")]
        public List<string>? DominantColors { get; init; }
    }

    private sealed class OpenRouterTag
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; } = 0.5;
    }
}
