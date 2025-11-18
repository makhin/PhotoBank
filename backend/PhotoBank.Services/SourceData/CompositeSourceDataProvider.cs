using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.SourceData;

/// <summary>
/// Composite provider that tries multiple source data providers in order
/// </summary>
public class CompositeSourceDataProvider : ISourceDataProvider
{
    private readonly IEnumerable<ISourceDataProvider> _providers;

    public CompositeSourceDataProvider(IEnumerable<ISourceDataProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public bool CanProvideData(Photo photo, Storage storage)
    {
        return _providers.Any(p => p.CanProvideData(photo, storage));
    }

    public async Task<SourceDataDto> GetSourceDataAsync(Photo photo, Storage storage, CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers)
        {
            if (provider.CanProvideData(photo, storage))
            {
                try
                {
                    return await provider.GetSourceDataAsync(photo, storage, cancellationToken);
                }
                catch
                {
                    // Try next provider if this one fails
                    continue;
                }
            }
        }

        throw new InvalidOperationException($"No source data provider could supply data for photo {photo?.Id}");
    }
}
