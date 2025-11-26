using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

/// <summary>
/// Enricher that detects NSFW content using local ONNX model
/// </summary>
public class NsfwEnricher : IEnricher
{
    private readonly INsfwDetector _detector;
    private readonly ILogger<NsfwEnricher> _logger;

    public EnricherType EnricherType => EnricherType.Nsfw;

    public Type[] Dependencies => new Type[] { typeof(PreviewEnricher) };

    public NsfwEnricher(INsfwDetector detector, ILogger<NsfwEnricher> logger)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (sourceData?.OriginalImage == null)
            {
                _logger.LogWarning("No original image available for photo {PhotoId}", photo.Id);
                return;
            }

            _logger.LogDebug("Running NSFW detection for photo {PhotoId}", photo.Id);

            // Convert IMagickImage to byte array for ONNX processing
            var imageBytes = sourceData.OriginalImage.ToByteArray();

            // Run detection asynchronously to avoid blocking
            var result = await Task.Run(() => _detector.Detect(imageBytes), cancellationToken);

            // Update photo properties
            photo.IsAdultContent = result.IsNsfw;
            photo.AdultScore = result.NsfwConfidence;
            photo.IsRacyContent = result.IsRacy;
            photo.RacyScore = result.RacyConfidence;

            _logger.LogDebug(
                "NSFW detection completed for photo {PhotoId}: IsNsfw={IsNsfw} (confidence={NsfwConfidence:F2}), IsRacy={IsRacy} (confidence={RacyConfidence:F2})",
                photo.Id, result.IsNsfw, result.NsfwConfidence, result.IsRacy, result.RacyConfidence);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("NSFW scores for photo {PhotoId}: {Scores}",
                    photo.Id,
                    string.Join(", ", result.Scores.Select(kvp => $"{kvp.Key}={kvp.Value:F3}")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during NSFW detection for photo {PhotoId}", photo.Id);
            throw;
        }
    }
}
