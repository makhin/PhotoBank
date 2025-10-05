using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace PhotoBank.Services.Internal;

internal sealed class CachedAsyncValue<T>
{
    private readonly IMemoryCache _cache;
    private readonly Func<object> _keyFactory;
    private readonly Func<ICacheEntry, Task<T>> _valueFactory;
    private readonly Func<IEnumerable<object>> _invalidationKeysFactory;
    private readonly object _syncRoot = new();
    private Lazy<Task<T>> _lazy;

    public CachedAsyncValue(
        IMemoryCache cache,
        Func<object> keyFactory,
        Func<ICacheEntry, Task<T>> valueFactory,
        Func<IEnumerable<object>>? invalidationKeysFactory = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _keyFactory = keyFactory ?? throw new ArgumentNullException(nameof(keyFactory));
        _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        _invalidationKeysFactory = invalidationKeysFactory ?? (() => Array.Empty<object>());

        _lazy = CreateLazy();
    }

    public Task<T> GetValueAsync()
    {
        var lazy = _lazy;
        return lazy.Value;
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            foreach (var key in _invalidationKeysFactory())
            {
                if (key is not null)
                {
                    _cache.Remove(key);
                }
            }

            _lazy = CreateLazy();
        }
    }

    private Lazy<Task<T>> CreateLazy()
        => new(() => _cache.GetOrCreateAsync(_keyFactory(), entry =>
        {
            entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                State = this,
                EvictionCallback = static (_, _, _, state) =>
                {
                    if (state is CachedAsyncValue<T> cached)
                    {
                        cached.HandleEvicted();
                    }
                }
            });

            return _valueFactory(entry);
        })!, LazyThreadSafetyMode.ExecutionAndPublication);

    private void HandleEvicted()
    {
        if (!Monitor.TryEnter(_syncRoot))
        {
            return;
        }

        try
        {
            _lazy = CreateLazy();
        }
        finally
        {
            Monitor.Exit(_syncRoot);
        }
    }
}
