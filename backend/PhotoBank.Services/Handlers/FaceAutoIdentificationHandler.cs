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
using static System.Math;

namespace PhotoBank.Services.Handlers;

/// <summary>
/// Automatically identifies faces after photo creation by matching them against known persons.
/// Uses embeddings already extracted during face detection (no duplicate API calls!).
/// </summary>
public class FaceAutoIdentificationHandler : INotificationHandler<PhotoCreated>
{
    private readonly IFaceProvider _faceProvider;
    private readonly IFaceEmbeddingRepository _embeddings;
    private readonly PhotoBankDbContext _context;
    private readonly LocalInsightFaceOptions _opts;
    private readonly ILogger<FaceAutoIdentificationHandler> _log;

    public FaceAutoIdentificationHandler(
        IFaceProvider faceProvider,
        IFaceEmbeddingRepository embeddings,
        PhotoBankDbContext context,
        IOptions<LocalInsightFaceOptions> opts,
        ILogger<FaceAutoIdentificationHandler> log)
    {
        _faceProvider = faceProvider;
        _embeddings = embeddings;
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
            // Load Face records with embeddings (already saved during detection!)
            var faceIds = notification.Faces.Select(f => f.FaceId).ToList();
            var faces = await _context.Faces
                .Where(f => faceIds.Contains(f.Id) && f.Embedding != null)
                .ToListAsync(cancellationToken);

            if (faces.Count == 0)
            {
                _log.LogWarning("No faces with embeddings found for photo {PhotoId}", notification.PhotoId);
                return;
            }

            _log.LogDebug("Found {Count} faces with embeddings for photo {PhotoId}", faces.Count, notification.PhotoId);

            var identifiedCount = 0;

            // Process each face with embedding
            foreach (var face in faces)
            {
                // Skip if already identified
                if (face.PersonId != null)
                {
                    _log.LogDebug("Face {FaceId} already assigned to person {PersonId}", face.Id, face.PersonId);
                    continue;
                }

                // Convert pgvector to float[] and normalize
                var embedding = face.Embedding.ToArray();
                var normalized = Normalize(embedding);

                // Search for similar faces in database
                var similarFaces = await _embeddings.FindSimilarFacesAsync(normalized, _opts.IdentifyMaxCandidates, cancellationToken);

                if (similarFaces.Count == 0)
                {
                    _log.LogDebug("No similar faces found for face {FaceId}", face.Id);
                    continue;
                }

                // Get best match (lowest distance = highest similarity)
                var bestMatch = similarFaces.First(); // Already sorted by distance
                var confidence = 1f - bestMatch.Distance; // Convert distance to confidence

                // Check if confidence meets threshold
                if (confidence < _opts.AutoIdentifyThreshold)
                {
                    _log.LogDebug(
                        "Face {FaceId} best match has confidence {Confidence:F3}, below threshold {Threshold:F3}",
                        face.Id, confidence, _opts.AutoIdentifyThreshold);
                    continue;
                }

                // Auto-assign face to person
                face.PersonId = bestMatch.PersonId;
                face.IdentityStatus = IdentityStatus.Identified;
                face.IdentifiedWithConfidence = confidence;
                face.Provider = "Local";
                face.ExternalId = $"local:{face.Id}";

                identifiedCount++;

                _log.LogInformation(
                    "Auto-identified face {FaceId} as person {PersonId} with confidence {Confidence:F3}",
                    face.Id, bestMatch.PersonId, confidence);
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

    private static float[] Normalize(float[] v)
    {
        var sum = 0f;
        for (int i = 0; i < v.Length; i++)
            sum += v[i] * v[i];
        var inv = 1f / (float)Sqrt(Max(sum, 1e-12f));
        var r = new float[v.Length];
        for (int i = 0; i < v.Length; i++)
            r[i] = v[i] * inv;
        return r;
    }
}
