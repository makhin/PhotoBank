using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

/// <summary>
/// Unified face enricher that works with any face recognition provider (Azure, AWS, Local)
/// through the IFaceProvider abstraction.
/// </summary>
public class UnifiedFaceEnricher : IEnricher
{
    private readonly UnifiedFaceService _faceService;
    private readonly IFacePreviewService _facePreviewService;
    private readonly ILogger<UnifiedFaceEnricher> _logger;

    public UnifiedFaceEnricher(
        UnifiedFaceService faceService,
        IFacePreviewService facePreviewService,
        ILogger<UnifiedFaceEnricher> logger)
    {
        _faceService = faceService;
        _facePreviewService = facePreviewService;
        _logger = logger;
    }

    public EnricherType EnricherType => EnricherType.Face;
    public Type[] Dependencies => new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) };

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
    {
        if (sourceData.PreviewImage is null)
        {
            _logger.LogDebug("No preview image available for photo {PhotoId}, skipping face detection", photo.Id);
            photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
            return;
        }

        try
        {
            var detectedFaces = await _faceService.DetectFacesAsync(
                sourceData.PreviewImage.ToByteArray(),
                cancellationToken);

            if (detectedFaces.Count == 0)
            {
                _logger.LogDebug("No faces detected in photo {PhotoId}", photo.Id);
                photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
                return;
            }

            _logger.LogInformation("Detected {FaceCount} face(s) in photo {PhotoId}", detectedFaces.Count, photo.Id);
            photo.FaceIdentifyStatus = FaceIdentifyStatus.Detected;
            photo.Faces = new List<Face>();

            foreach (var detectedFace in detectedFaces)
            {
                try
                {
                    await ProcessDetectedFaceAsync(photo, sourceData, detectedFace, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to process detected face {FaceId} for photo {PhotoId}, skipping this face",
                        detectedFace.ProviderFaceId, photo.Id);
                    // Continue processing other faces
                }
            }

            if (photo.Faces.Count == 0)
            {
                _logger.LogWarning("All detected faces failed to process for photo {PhotoId}", photo.Id);
                photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting faces for photo {PhotoId}", photo.Id);
            photo.FaceIdentifyStatus = FaceIdentifyStatus.ProcessingError;
            throw;
        }
    }

    private async Task ProcessDetectedFaceAsync(
        Photo photo,
        SourceDataDto sourceData,
        FaceRecognition.Abstractions.DetectedFaceDto detectedFace,
        CancellationToken cancellationToken)
    {
        // Create face preview - we need to convert from provider-specific format
        // For now, we'll use a simple approach - in future iterations, this can be enhanced
        // to use bounding box from DetectedFaceDto when available
        byte[] facePreviewBytes = await CreateFacePreviewAsync(sourceData.PreviewImage, detectedFace);
        sourceData.FaceImages.Add(facePreviewBytes);

        var face = new Face
        {
            PhotoId = photo.Id,
            IdentityStatus = IdentityStatus.NotIdentified,
            // Rectangle will need to be set based on provider-specific data
            // For now, we'll leave it null - this is a known limitation that should be addressed
            Rectangle = null,
            Age = detectedFace.Age,
            Gender = ConvertGender(detectedFace.Gender),
            Smile = null, // Not all providers return smile
            FaceAttributes = JsonConvert.SerializeObject(new
            {
                detectedFace.ProviderFaceId,
                detectedFace.Confidence,
                detectedFace.Age,
                detectedFace.Gender
            })
        };

        photo.Faces.Add(face);

        _logger.LogDebug(
            "Processed face {FaceId} for photo {PhotoId}: Age={Age}, Gender={Gender}, Confidence={Confidence}",
            detectedFace.ProviderFaceId, photo.Id, detectedFace.Age, detectedFace.Gender, detectedFace.Confidence);
    }

    private async Task<byte[]> CreateFacePreviewAsync(
        IMagickImage<byte> previewImage,
        FaceRecognition.Abstractions.DetectedFaceDto detectedFace)
    {
        // TODO: Use bounding box from detectedFace when it's added to DetectedFaceDto
        // For now, return the whole preview image as a temporary solution
        // This is a known limitation that will be addressed in future iterations

        using var memoryStream = new System.IO.MemoryStream();
        await previewImage.WriteAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private static bool? ConvertGender(string? gender)
    {
        if (string.IsNullOrEmpty(gender))
            return null;

        return string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase);
    }
}
