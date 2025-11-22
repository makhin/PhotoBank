using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.FaceRecognition.Local;
using Pgvector;

namespace PhotoBank.Services.Recognition
{
    public interface IRecognitionService
    {
        Task RegisterPersonsAsync(CancellationToken ct = default);
        Task MigrateEmbeddingsAsync(CancellationToken ct = default);
    }

    public class RecognitionService : IRecognitionService
    {
        private readonly IRepository<Face> _faces;
        private readonly ILocalInsightFaceClient _client;
        private readonly MinioObjectService _minioObjectService;
        private readonly ILogger<RecognitionService> _logger;

        public RecognitionService(
            IRepository<Face> faces,
            ILocalInsightFaceClient client,
            MinioObjectService minioObjectService,
            ILogger<RecognitionService> logger)
        {
            _faces = faces;
            _client = client;
            _minioObjectService = minioObjectService;
            _logger = logger;
        }

        public async Task RegisterPersonsAsync(CancellationToken ct = default)
        {
            var facesToProcess = _faces.GetByCondition(p => p.PersonId != null && !string.IsNullOrEmpty(p.S3Key_Image)).ToList();
            _logger.LogInformation("Processing {Count} faces for embedding extraction", facesToProcess.Count);

            foreach (var face in facesToProcess)
            {
                try
                {
                    var bytes = await _minioObjectService.GetObjectAsync(face.S3Key_Image);
                    await using var fileStream = new MemoryStream(bytes);

                    // Extract embedding from cropped face image
                    var response = await _client.EmbedAsync(fileStream, includeAttributes: false, ct);

                    // Save embedding to Face entity
                    face.Embedding = new Vector(response.FlatEmbedding);
                    await _faces.UpdateAsync(face);

                    _logger.LogInformation("Successfully extracted embedding for face {FaceId} (person {PersonId})",
                        face.Id, face.PersonId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract embedding for face {FaceId}", face.Id);
                }
            }
        }

        /// <summary>
        /// Migrates face embeddings for all faces that don't have embeddings yet.
        /// Loads cropped face images from S3, extracts embeddings via InsightFace API, and saves them.
        /// </summary>
        public async Task MigrateEmbeddingsAsync(CancellationToken ct = default)
        {
            // Find all faces without embeddings that have S3 images
            var facesWithoutEmbeddings = _faces.GetByCondition(f =>
                f.Embedding == null &&
                !string.IsNullOrEmpty(f.S3Key_Image))
                .ToList();

            _logger.LogInformation("Found {Count} faces without embeddings to process", facesWithoutEmbeddings.Count);

            if (facesWithoutEmbeddings.Count == 0)
            {
                _logger.LogInformation("No faces to migrate");
                return;
            }

            var processed = 0;
            var failed = 0;

            foreach (var face in facesWithoutEmbeddings)
            {
                try
                {
                    _logger.LogDebug("Processing face {FaceId} (PersonId: {PersonId})", face.Id, face.PersonId);

                    // Load cropped face image from S3
                    var bytes = await _minioObjectService.GetObjectAsync(face.S3Key_Image);
                    await using var fileStream = new MemoryStream(bytes);

                    // Extract embedding from InsightFace API
                    var response = await _client.EmbedAsync(fileStream, includeAttributes: false, ct);

                    // Save embedding to database
                    face.Embedding = new Vector(response.FlatEmbedding);
                    await _faces.UpdateAsync(face);

                    processed++;
                    _logger.LogInformation(
                        "Successfully migrated embedding for face {FaceId} (PersonId: {PersonId}) - {Processed}/{Total}",
                        face.Id, face.PersonId, processed, facesWithoutEmbeddings.Count);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to migrate embedding for face {FaceId}", face.Id);
                }
            }

            _logger.LogInformation(
                "Embedding migration completed. Processed: {Processed}, Failed: {Failed}, Total: {Total}",
                processed, failed, facesWithoutEmbeddings.Count);
        }
    }
}
