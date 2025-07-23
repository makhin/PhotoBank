using System;
using System.IO;
using System.Threading.Tasks;
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

        public RecognitionService(IRepository<Face> faces, IInsightFaceApiClient client)
        {
            _faces = faces;
            _client = client;
        }

        public async Task RegisterPersonsAsync()
        {
            foreach (var face in _faces.GetByCondition(p => p.PersonId == 1))
            {
                var fileStream = new MemoryStream(face.Image);
                await _client.RegisterAsync(face.PersonId.Value, fileStream).ContinueWith(task =>
                {
                    Console.WriteLine(task.IsCompletedSuccessfully
                        ? $"Recognized: {task.Result}"
                        : $"Recognition failed for {face.Id}: {task.Exception?.Message}");
                });
            }
        }

    }
}
