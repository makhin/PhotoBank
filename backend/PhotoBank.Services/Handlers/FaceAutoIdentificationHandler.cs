using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Events;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Local;

namespace PhotoBank.Services.Handlers;

/// <summary>
/// Automatically identifies faces after photo creation by matching them against known persons.
/// Triggers after PhotoCreatedHandler completes S3 upload.
/// </summary>
public class FaceAutoIdentificationHandler : INotificationHandler<PhotoCreated>
{
    private readonly IFaceProvider _faceProvider;
    private readonly PhotoBankDbContext _context;
    private readonly LocalInsightFaceOptions _opts;
    private readonly ILogger<FaceAutoIdentificationHandler> _log;

    public FaceAutoIdentificationHandler(
        IFaceProvider faceProvider,
        PhotoBankDbContext context,
        IOptions<LocalInsightFaceOptions> opts,
        ILogger<FaceAutoIdentificationHandler> log)
    {
        _faceProvider = faceProvider;
        _context = context;
        _opts = opts.Value;
        _log = log;
    }

    public async Task Handle(PhotoCreated notification, CancellationToken cancellationToken)
    {
        // Only run for Local InsightFace provider
        if (_faceProvider.Kind != FaceProviderKind.Local)
        {
            _log.LogDebug("Skipping auto-identification for provider {Provider}", _faceProvider.Kind);
            return;
        }

        if (notification.Faces == null || notification.Faces.Count == 0)
        {
            _log.LogDebug("No faces in photo {PhotoId}, skipping auto-identification", notification.PhotoId);
            return;
        }

        _log.LogInformation("Starting auto-identification for {Count} face(s) in photo {PhotoId}",
            notification.Faces.Count, notification.PhotoId);

        try
        {
            // Build provider face IDs ("local:123")
            var providerFaceIds = notification.Faces
                .Select(f => $"local:{f.FaceId}")
                .ToList();

            // Call IdentifyAsync to find matching persons
            var identifyResults = await _faceProvider.IdentifyAsync(providerFaceIds, cancellationToken);

            var identifiedCount = 0;
            foreach (var result in identifyResults)
            {
                if (result.Candidates == null || result.Candidates.Count == 0)
                    continue;

                // Get the best candidate
                var bestCandidate = result.Candidates
                    .OrderByDescending(c => c.Confidence)
                    .First();

                // Check if confidence is high enough for auto-identification
                if (bestCandidate.Confidence < _opts.AutoIdentifyThreshold)
                {
                    _log.LogDebug(
                        "Face {FaceId} best match has confidence {Confidence:F3}, below threshold {Threshold:F3}",
                        result.ProviderFaceId, bestCandidate.Confidence, _opts.AutoIdentifyThreshold);
                    continue;
                }

                // Parse face ID and person ID
                var faceId = int.Parse(result.ProviderFaceId.Replace("local:", ""));
                var personId = int.Parse(bestCandidate.ProviderPersonId.Replace("local:", ""));

                // Update face with person assignment
                var face = await _context.Faces.FindAsync(new object[] { faceId }, cancellationToken);
                if (face == null)
                {
                    _log.LogWarning("Face {FaceId} not found in database", faceId);
                    continue;
                }

                // Check if face is already identified
                if (face.PersonId != null)
                {
                    _log.LogDebug("Face {FaceId} already assigned to person {PersonId}", faceId, face.PersonId);
                    continue;
                }

                // Auto-assign face to person
                face.PersonId = personId;
                face.IdentityStatus = IdentityStatus.Identified;
                face.IdentifiedWithConfidence = bestCandidate.Confidence;
                face.Provider = "Local";
                face.ExternalId = $"local:{faceId}";

                identifiedCount++;

                _log.LogInformation(
                    "Auto-identified face {FaceId} as person {PersonId} with confidence {Confidence:F3}",
                    faceId, personId, bestCandidate.Confidence);
            }

            if (identifiedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _log.LogInformation(
                    "Auto-identification completed for photo {PhotoId}: {IdentifiedCount}/{TotalCount} faces identified",
                    notification.PhotoId, identifiedCount, notification.Faces.Count);
            }
            else
            {
                _log.LogInformation(
                    "No faces met auto-identification threshold for photo {PhotoId}",
                    notification.PhotoId);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error during auto-identification for photo {PhotoId}", notification.PhotoId);
            // Don't throw - this is a best-effort feature
        }
    }
}
