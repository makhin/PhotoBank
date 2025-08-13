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

/// <summary>
/// Executes enrichers in dependency order with logging, cancellation and error handling.
/// </summary>
public sealed class EnrichmentPipeline : IEnrichmentPipeline
{
    private readonly IServiceProvider _rootProvider;
    private readonly ILogger<EnrichmentPipeline> _logger;
    private readonly EnrichmentPipelineOptions _options;

    private readonly Type[] _orderedTypes;
    private readonly Func<IServiceProvider, IEnricher>[] _factories;

    public EnrichmentPipeline(
        IServiceProvider rootProvider,
        IEnumerable<Type> enricherTypes,
        IOptions<EnrichmentPipelineOptions> options,
        ILogger<EnrichmentPipeline> logger)
    {
        _rootProvider = rootProvider;
        _logger = logger;
        _options = options.Value;

        var types = enricherTypes.Distinct().ToArray();
        if (types.Length == 0)
            throw new InvalidOperationException("No IEnricher implementations were provided to the pipeline.");

        _orderedTypes = EnricherDependencyResolver.Sort(types, rootProvider);
        _factories = _orderedTypes.Select(CreateFactory).ToArray();

        _logger.LogInformation("EnrichmentPipeline initialized with {Count} enrichers: {Names}",
            _orderedTypes.Length, string.Join(", ", _orderedTypes.Select(t => t.Name)));
    }

    public async Task RunAsync(Photo photo, SourceDataDto source, CancellationToken ct = default)
    {
        using var scope = _rootProvider.CreateScope();
        var sp = scope.ServiceProvider;

        for (var i = 0; i < _factories.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            var enricher = _factories[i](sp);
            var stepName = enricher.GetType().Name;

            var sw = _options.LogTimings ? Stopwatch.StartNew() : null;
            try
            {
                _logger.LogDebug("➡️ Starting enricher: {Step}", stepName);
                await enricher.EnrichAsync(photo, source, ct);
                if (_options.LogTimings)
                    _logger.LogInformation("✅ {Step} completed in {Ms} ms", stepName, sw!.Elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏹️ Pipeline canceled during {Step}", stepName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Enricher {Step} failed", stepName);
                if (!_options.ContinueOnError)
                    throw;
            }
            finally
            {
                sw?.Stop();
            }
        }
    }

    public async Task RunBatchAsync(IEnumerable<(Photo photo, SourceDataDto source)> items, CancellationToken ct = default)
    {
        var list = items as (Photo photo, SourceDataDto source)[] ?? items.ToArray();
        if (list.Length == 0) return;

        var dop = _options.MaxDegreeOfParallelism.HasValue && _options.MaxDegreeOfParallelism > 0
            ? _options.MaxDegreeOfParallelism.Value
            : Environment.ProcessorCount;

        _logger.LogInformation("Batch pipeline started for {Count} items with DOP={Dop}", list.Length, dop);

        await Parallel.ForEachAsync(list, new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = dop
        }, async (item, token) =>
        {
            await RunAsync(item.photo, item.source, token);
        });

        _logger.LogInformation("Batch pipeline completed for {Count} items", list.Length);
    }

    private static Func<IServiceProvider, IEnricher> CreateFactory(Type t) =>
        sp => (IEnricher)sp.GetRequiredService(t);
}

