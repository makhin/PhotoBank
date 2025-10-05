using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.Services.Enrichment;

public sealed class EnricherTypeCatalog
{
    public EnricherTypeCatalog(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        Types = types.Distinct().ToArray();
    }

    public IReadOnlyList<Type> Types { get; }
}
