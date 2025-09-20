using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace PhotoBank.Services.Internal;

internal static class CacheHelper
{
    internal static MemoryCacheEntryOptions CacheOptions { get; } = new()
    {
        AbsoluteExpiration = null,
        SlidingExpiration = null
    };

    internal static async Task<TItem> GetOrCreateAsync<TItem>(
        this IMemoryCache cache,
        object key,
        Func<Task<TItem>> factory,
        MemoryCacheEntryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(factory);

        if (!cache.TryGetValue(key, out TItem? value))
        {
            value = await factory().ConfigureAwait(false);
            cache.Set(key, value, options ?? CacheOptions);
        }

        return value;
    }
}
