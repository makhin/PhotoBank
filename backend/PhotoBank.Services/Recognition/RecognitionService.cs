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
    }
}
