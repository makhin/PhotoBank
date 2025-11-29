using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

/// <summary>
/// Enricher that detects adult content using NudeNet ONNX model
/// </summary>
public class AdultEnricher : IEnricher
{
    private readonly INudeNetDetector _detector;
    private readonly ILogger<AdultEnricher> _logger;

    public EnricherType EnricherType => EnricherType.Adult;

    public Type[] Dependencies => [typeof(PreviewEnricher)];

    public AdultEnricher(INudeNetDetector detector, ILogger<AdultEnricher> logger)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
    {
        if (sourceData?.LetterboxedImage640 == null)
        {
            _logger.LogWarning("No letterboxed image available for NudeNet detection for photo {PhotoId}", photo.Id);
            return;
        }

        if (sourceData?.OriginalImage == null)
        {
            _logger.LogWarning("No original image available for NudeNet detection for photo {PhotoId}", photo.Id);
            return;
        }

        try
        {
            _logger.LogDebug("Running NudeNet detection for photo {PhotoId}", photo.Id);

            // Run detection asynchronously to avoid blocking
            // Use pre-prepared letterboxed image and parameters from PreviewEnricher
            var result = await Task.Run(() => _detector.Detect(
                letterboxedImage: sourceData.LetterboxedImage640,
                originalWidth: (int)sourceData.OriginalImage.Width,
                originalHeight: (int)sourceData.OriginalImage.Height,
                letterboxScale: sourceData.LetterboxScale,
                padX: sourceData.LetterboxPadX,
                padY: sourceData.LetterboxPadY), cancellationToken);

            // Update photo properties
            photo.IsAdultContent = result.IsNsfw;
            photo.AdultScore = result.NsfwConfidence;
            photo.IsRacyContent = result.IsRacy;
            photo.RacyScore = result.RacyConfidence;

            _logger.LogDebug(
                "NudeNet detection completed for photo {PhotoId}: IsNsfw={IsNsfw} (confidence={NsfwConfidence:F2}), IsRacy={IsRacy} (confidence={RacyConfidence:F2}), Detections={DetectionCount}",
                photo.Id, result.IsNsfw, result.NsfwConfidence, result.IsRacy, result.RacyConfidence, result.Detections.Count);

            if (_logger.IsEnabled(LogLevel.Trace) && result.DetectionCounts.Any())
            {
                _logger.LogTrace("NudeNet detections for photo {PhotoId}: {Detections}",
                    photo.Id,
                    string.Join(", ", result.DetectionCounts.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during NudeNet detection for photo {PhotoId}", photo.Id);
            throw;
        }
    }
}
