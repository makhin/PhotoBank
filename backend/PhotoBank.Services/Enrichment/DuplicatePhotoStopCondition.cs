using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Photos;

namespace PhotoBank.Services.Enrichment;

public sealed class DuplicatePhotoStopCondition : IEnrichmentStopCondition
{
    private const int ExactDuplicateThreshold = 0;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DuplicatePhotoStopCondition> _logger;

    public DuplicatePhotoStopCondition(IServiceProvider serviceProvider, ILogger<DuplicatePhotoStopCondition> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyCollection<Type> AppliesAfterEnrichers { get; } = new[] { typeof(PreviewEnricher) };

    public async Task<string?> GetStopReasonAsync(EnrichmentContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.Photo.ImageHash))
        {
            _logger.LogDebug("Skip duplicate check: image hash is missing");
            return null;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var duplicateFinder = scope.ServiceProvider.GetRequiredService<IPhotoDuplicateFinder>();

        var duplicates = (await duplicateFinder.FindDuplicatesAsync(
                id: null,
                hash: context.Photo.ImageHash,
                threshold: ExactDuplicateThreshold,
                cancellationToken: cancellationToken))
            .ToList();

        if (duplicates.Count == 0)
        {
            return null;
        }

        var duplicatesSummary = string.Join(", ", duplicates.Select(d => $"{d.Id} ({d.StorageName}/{d.RelativePath})"));
        return $"Duplicate photo detected. Existing matches: {duplicatesSummary}";
    }
}
