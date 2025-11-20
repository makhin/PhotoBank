using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichment;

public sealed class EnrichmentPipeline : IEnrichmentPipeline
{
    private readonly IServiceProvider _root;
    private readonly Type[] _enricherTypes;
    private readonly EnrichmentPipelineOptions _opts;
    private readonly ILogger<EnrichmentPipeline> _log;

    public EnrichmentPipeline(IServiceProvider root,
                              EnricherTypeCatalog enricherCatalog,
                              IOptions<EnrichmentPipelineOptions> opts,
                              ILogger<EnrichmentPipeline> log)
    {
        _root = root;
        _enricherTypes = enricherCatalog.Types.ToArray();
        _opts = opts.Value;
        _log = log;
    }

    public Task RunAsync(Photo photo, SourceDataDto source, CancellationToken ct = default) =>
        RunAsync(photo, source, _enricherTypes, ct);

    public Task RunAsync(Photo photo, SourceDataDto source, IReadOnlyCollection<Type> enrichers, CancellationToken ct = default) =>
        RunAsync(photo, source, enrichers, null, ct);

    public async Task RunAsync(Photo photo, SourceDataDto source, IReadOnlyCollection<Type> enrichers, IServiceProvider? serviceProvider, CancellationToken ct = default)
    {
        // If a service provider is provided, use it directly (for transaction participation).
        // Otherwise, create a new scope to isolate enricher resolution.
        IServiceScope? scope = null;
        IServiceProvider provider;

        if (serviceProvider != null)
        {
            // Use provided service provider - enrichers will share the caller's DbContext
            // and participate in any active transaction
            provider = serviceProvider;
        }
        else
        {
            // Create new scope - enrichers get fresh DbContext instances
            scope = _root.CreateScope();
            provider = scope.ServiceProvider;
        }

        try
        {
            var ordered = TopoSort(enrichers.ToArray(), provider);
            foreach (var t in ordered)
            {
                ct.ThrowIfCancellationRequested();

                var enricher = (IEnricher)provider.GetRequiredService(t);
                var sw = _opts.LogTimings ? Stopwatch.StartNew() : null;

                try
                {
                    await enricher.EnrichAsync(photo, source, ct);
                    photo.EnrichedWithEnricherType |= enricher.EnricherType;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Enricher {Enricher} failed", t.Name);
                    if (!_opts.ContinueOnError) throw;
                }
                finally
                {
                    if (sw is { })
                        _log.LogInformation("Enricher {Enricher} took {Ms} ms", t.Name, sw.ElapsedMilliseconds);
                }
            }
        }
        finally
        {
            scope?.Dispose();
        }
    }

    public async Task RunBatchAsync(IEnumerable<(Photo photo, SourceDataDto source)> items, CancellationToken ct = default)
    {
        var arr = items.ToArray();
        var configuredDop = _opts.MaxDegreeOfParallelism;
        var dop = configuredDop.HasValue && configuredDop.Value > 0
            ? configuredDop.Value
            : Math.Max(1, Environment.ProcessorCount);
        _log.LogInformation("Batch enrichment: {Count} items, DOP={Dop}", arr.Length, dop);

        await Parallel.ForEachAsync(arr, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = ct },
            async (it, token) => await RunAsync(it.photo, it.source, token));
    }

    private static Type[] TopoSort(IReadOnlyList<Type> types, IServiceProvider sp)
    {
        // зависимости из интерфейса и [DependsOn]
        var edges = new Dictionary<Type, HashSet<Type>>();
        foreach (var t in types)
        {
            var deps = new HashSet<Type>(
                ((IOrderDependent)sp.GetRequiredService(t)).Dependencies ?? Array.Empty<Type>());

            foreach (var a in t.GetCustomAttributes(typeof(DependsOnAttribute), inherit: true).Cast<DependsOnAttribute>())
                deps.Add(a.EnricherType);

            edges[t] = deps;
        }

        var result = new List<Type>();
        var temp = new HashSet<Type>();
        var perm = new HashSet<Type>();

        void Visit(Type v)
        {
            if (perm.Contains(v)) return;
            if (temp.Contains(v))
                throw new InvalidOperationException($"Dependency cycle detected around {v.Name}");

            temp.Add(v);
            foreach (var u in edges[v])
            {
                if (!edges.ContainsKey(u))
                    throw new InvalidOperationException($"Unknown dependency {u.Name} of {v.Name}");
                Visit(u);
            }
            temp.Remove(v);
            perm.Add(v);
            result.Add(v);
        }

        foreach (var t in types) Visit(t);
        return result.ToArray();
    }
}