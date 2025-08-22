using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace PhotoBank.Services.Translator;

public sealed class TranslatorService(HttpClient http, IOptions<TranslatorOptions> opts) : ITranslatorService
{
    private readonly HttpClient _http = http;
    private readonly TranslatorOptions _opts = opts.Value;

    public async Task<TranslateResponse> TranslateAsync(TranslateRequest req, CancellationToken ct = default)
    {
        if (req.Texts is null || req.Texts.Length == 0)
            return new TranslateResponse(Array.Empty<string>());

        var query = new List<string> { "api-version=3.0", $"to={Uri.EscapeDataString(req.To)}" };
        if (!string.IsNullOrWhiteSpace(req.From)) query.Add($"from={Uri.EscapeDataString(req.From)}");
        if (!string.IsNullOrWhiteSpace(req.TextType)) query.Add($"textType={req.TextType}");
        if (!string.IsNullOrWhiteSpace(req.ProfanityAction)) query.Add($"profanityAction={req.ProfanityAction}");
        if (!string.IsNullOrWhiteSpace(req.Category)) query.Add($"category={Uri.EscapeDataString(req.Category)}");

        var url = $"{_opts.Endpoint.TrimEnd('/')}/translate?{string.Join("&", query)}";

        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(req.Texts.Select(t => new { Text = t }))
        };

        message.Headers.Add("Ocp-Apim-Subscription-Key", _opts.Key);
        message.Headers.Add("Ocp-Apim-Subscription-Region", _opts.Region);

        using var resp = await _http.SendAsync(message, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Translator error {(int)resp.StatusCode}: {payload}");

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var result = new List<string>(req.Texts.Length);
        foreach (var item in root.EnumerateArray())
        {
            var tr = item.GetProperty("translations")[0];
            result.Add(tr.GetProperty("text").GetString() ?? "");
        }

        return new TranslateResponse(result.ToArray());
    }
}

