using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace PhotoBank.UnitTests.Infrastructure.Http;

internal sealed record HttpRequestSnapshot(HttpMethod Method, string Path, string Query, IReadOnlyDictionary<string, string[]> Headers, string? Body)
{
    public static HttpRequestSnapshot Capture(HttpRequestMessage request)
    {
        var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        string? body = null;

        if (request.Content != null)
        {
            body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        return new HttpRequestSnapshot(
            request.Method,
            request.RequestUri?.AbsolutePath ?? string.Empty,
            request.RequestUri?.Query ?? string.Empty,
            headers,
            body);
    }
}
