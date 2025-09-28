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
    IMemoryCache cache,
    ISearchReferenceDataService referenceDataService
) : ISearchFilterNormalizer
{
    public async Task<FilterDto> NormalizeAsync(FilterDto filter, CancellationToken ct = default)
    {
        var items = new List<(string Value, Action<string> Apply)>(capacity: (filter.TagNames?.Length ?? 0) + 1);

        if (!string.IsNullOrWhiteSpace(filter.Caption) && LanguageDetector.DetectRuEn(filter.Caption) == "ru")
        {
            var original = filter.Caption!;
            items.Add((original, translated => filter.Caption = translated));
        }

        if (filter.TagNames is { Length: > 0 })
        {
            for (int i = 0; i < filter.TagNames.Length; i++)
            {
                var name = filter.TagNames[i];
                if (!string.IsNullOrWhiteSpace(name) && LanguageDetector.DetectRuEn(name) == "ru")
                {
                    var index = i;
                    items.Add((name, translated => filter.TagNames[index] = translated));
                }
            }
        }

        if (items.Count > 0)
        {
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
        }

        await PopulateIdsAsync(
            filter.PersonNames,
            referenceDataService.GetPersonsAsync,
            p => p.Id,
            p => p.Name,
            ids => filter.Persons = ids,
            ct
        );

        await PopulateIdsAsync(
            filter.TagNames,
            referenceDataService.GetTagsAsync,
            t => t.Id,
            t => t.Name,
            ids => filter.Tags = ids,
            ct
        );

        return filter;
    }

    private static async Task PopulateIdsAsync<T>(
        string[]? names,
        Func<CancellationToken, Task<IReadOnlyList<T>>> getAll,
        Func<T, int> idSelector,
        Func<T, string> nameSelector,
        Action<int[]> apply,
        CancellationToken ct)
    {
        if (names is not { Length: > 0 }) return;

        var items = await getAll(ct);
        var lookup = items.Select(i => (Id: idSelector(i), Name: nameSelector(i)));

        var ids = MatchIds(names, lookup);
        if (ids.Length > 0)
            apply(ids);
    }

    private static int[] MatchIds(IEnumerable<string> names, IEnumerable<(int Id, string Name)> dictionary)
    {
        var ids = new List<int>();
        var candidates = dictionary
            .Select(item => (item.Id, Normalized: NormalizeName(item.Name)))
            .Where(item => item.Normalized.Length > 0)
            .ToArray();

        foreach (var name in names)
        {
            var normalizedQuery = NormalizeName(name);
            if (normalizedQuery.Length == 0)
                continue;

            var exact = candidates.FirstOrDefault(item => item.Normalized.Equals(normalizedQuery, StringComparison.Ordinal));
            if (exact.Normalized is not null)
            {
                ids.Add(exact.Id);
                continue;
            }

            var partialMatch = candidates
                .Where(item => item.Normalized.Contains(normalizedQuery, StringComparison.Ordinal)
                               || normalizedQuery.Contains(item.Normalized, StringComparison.Ordinal))
                .Select(item => new
                {
                    item.Id,
                    item.Normalized,
                    Distance = Levenshtein(item.Normalized, normalizedQuery)
                })
                .OrderBy(x => x.Distance)
                .ThenBy(x => Math.Abs(x.Normalized.Length - normalizedQuery.Length))
                .FirstOrDefault();

            if (partialMatch is not null)
            {
                // Ambiguous partial matches map to the closest known person to avoid over-filtering.
                ids.Add(partialMatch.Id);
                continue;
            }

            var match = candidates
                .Select(item => new { item.Id, Distance = Levenshtein(item.Normalized, normalizedQuery) })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (match is { Distance: <= 2 })
                ids.Add(match.Id);
        }

        return ids.Distinct().ToArray();
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var span = value.AsSpan().Trim();
        while (span.Length > 0 && span[0] == '@')
        {
            span = span[1..].TrimStart();
        }

        var builder = new StringBuilder(span.Length);
        foreach (var ch in span)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }

    private static string CacheKey(string text)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("ru->en|" + text));
        return "translate:" + Convert.ToHexString(bytes);
    }

    private static int Levenshtein(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;
        var d = new int[s.Length + 1, t.Length + 1];
        for (var i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) d[0, j] = j;
        for (var i = 1; i <= s.Length; i++)
        {
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }
        return d[s.Length, t.Length];
    }
}

