using System;
using System.IO;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
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
        private readonly IMinioClient _minioClient;

        public RecognitionService(IRepository<Face> faces, IInsightFaceApiClient client, IMinioClient minioClient)
        {
            _faces = faces;
            _client = client;
            _minioClient = minioClient;
        }

        public async Task RegisterPersonsAsync()
        {
            foreach (var face in _faces.GetByCondition(p => p.PersonId == 1))
            {
                if (string.IsNullOrEmpty(face.S3Key_Image))
                {
                    continue;
                }

                var bytes = await GetObjectAsync(face.S3Key_Image);
                await using var fileStream = new MemoryStream(bytes);
                await _client.RegisterAsync(face.PersonId!.Value, fileStream).ContinueWith(task =>
                {
                    Console.WriteLine(task.IsCompletedSuccessfully
                        ? $"Recognized: {task.Result}"
                        : $"Recognition failed for {face.Id}: {task.Exception?.Message}");
                });
            }
        }

        private async Task<byte[]> GetObjectAsync(string key)
        {
            using var ms = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket("photobank")
                .WithObject(key)
                .WithCallbackStream(stream => stream.CopyTo(ms)));
            return ms.ToArray();
        }

    }
}
