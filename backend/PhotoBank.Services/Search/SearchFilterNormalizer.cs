using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using PhotoBank.Services.Translator;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Search;

public sealed class SearchFilterNormalizer(
    ITranslatorService translator,
    IMemoryCache cache
) : ISearchFilterNormalizer
{
    public async Task<FilterDto> NormalizeAsync(FilterDto filter, CancellationToken ct = default)
    {
        var items = new List<(string Value, Action<string> Apply)>(capacity: 1);

        if (!string.IsNullOrWhiteSpace(filter.Caption) && LanguageDetector.DetectRuEn(filter.Caption) == "ru")
        {
            var original = filter.Caption!;
            items.Add((original, translated => filter.Caption = translated));
        }

        if (items.Count == 0) return filter;

        var unique = items.Select(x => x.Value).Distinct(StringComparer.Ordinal).ToArray();

        var toTranslate = new List<string>(unique.Length);
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var phrase in unique)
        {
            var key = CacheKey(phrase);
            if (cache.TryGetValue<string>(key, out var cached))
            {
                resolved[phrase] = cached;
            }
            else
            {
                toTranslate.Add(phrase);
            }
        }

        if (toTranslate.Count > 0)
        {
            var result = await translator.TranslateAsync(
                new TranslateRequest(
                    Texts: toTranslate.ToArray(),
                    To: "en",
                    From: "ru",
                    TextType: "plain"
                ),
                ct
            );

            for (int i = 0; i < toTranslate.Count; i++)
            {
                var src = toTranslate[i];
                var dst = result.Translations[i];

                resolved[src] = dst;
                cache.Set(CacheKey(src), dst, TimeSpan.FromHours(12));
            }
        }

        foreach (var (value, apply) in items)
        {
            var translated = resolved[value];
            apply(translated);
        }

        return filter;
    }

    private static string CacheKey(string text)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("ru->en|" + text));
        return "translate:" + Convert.ToHexString(bytes);
    }
}
