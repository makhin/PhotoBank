using System;
using System.IO;
using System.Threading.Tasks;
using PhotoBank.Services;
using PhotoBank.DbContext.Models;
using PhotoBank.InsightFaceApiClient;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Recognition
{
    public interface IRecognitionService
    {
        Task RegisterPersonsAsync();
    }

    public class RecognitionService : IRecognitionService
    {
        private readonly IRepository<Face> _faces;
        private readonly IInsightFaceApiClient _client;
        private readonly MinioObjectService _minioObjectService;

        public RecognitionService(IRepository<Face> faces, IInsightFaceApiClient client, MinioObjectService minioObjectService)
        {
            _faces = faces;
            _client = client;
            _minioObjectService = minioObjectService;
        }

        public async Task RegisterPersonsAsync()
        {
            foreach (var face in _faces.GetByCondition(p => p.PersonId == 1))
            {
                if (string.IsNullOrEmpty(face.S3Key_Image))
                {
                    continue;
                }

                var bytes = await _minioObjectService.GetObjectAsync(face.S3Key_Image);
                await using var fileStream = new MemoryStream(bytes);
                await _client.RegisterAsync(face.PersonId!.Value, fileStream).ContinueWith(task =>
                {
                    Console.WriteLine(task.IsCompletedSuccessfully
                        ? $"Recognized: {task.Result}"
                        : $"Recognition failed for {face.Id}: {task.Exception?.Message}");
                });
            }
        }

    }
}
