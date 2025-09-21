using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.UnitTests.Infrastructure.Http;

internal static class QueryStringParser
{
    public static IReadOnlyDictionary<string, List<string>> Parse(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        return query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .GroupBy(parts => Uri.UnescapeDataString(parts[0]), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }
}
