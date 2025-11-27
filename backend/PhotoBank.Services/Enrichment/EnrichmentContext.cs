using System;

using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichment;

public sealed class EnrichmentContext
{
    public EnrichmentContext(Photo photo, SourceDataDto source)
    {
        Photo = photo ?? throw new ArgumentNullException(nameof(photo));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Photo Photo { get; }

    public SourceDataDto Source { get; }

    public string? StopReason { get; private set; }

    public bool IsStopped => StopReason is not null;

    public bool TryStop(string reason)
    {
        if (IsStopped) return false;

        StopReason = reason;
        return true;
    }
}

public interface IEnrichmentContextAccessor
{
    EnrichmentContext? Current { get; set; }
}

public sealed class EnrichmentContextAccessor : IEnrichmentContextAccessor
{
    public EnrichmentContext? Current { get; set; }
}
