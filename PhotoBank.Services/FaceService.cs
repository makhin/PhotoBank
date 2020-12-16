using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.Services
{
    public interface IFaceService
    {
        void FindSimilarFaxes();
    }

    public class FaceService : IFaceService
    {
        private readonly IFaceClient _faceClient;
        private readonly IRepository<Face> _repository;

        public FaceService(IFaceClient faceClient, IRepository<Face> repository)
        {
            _faceClient = faceClient;
            _repository = repository;
        }

        public void FindSimilarFaxes()
        {
            string faceListId = Guid.NewGuid().ToString();
            Console.WriteLine(faceListId);
            _faceClient.FaceList.CreateAsync(faceListId, "Test", null, RecognitionModel.Recognition03).Wait();

            var faces = _repository.GetAll();
            var persistedFaces = new List<PersistedFace>();

            foreach (var face in faces)
            {
                if (Math.Sqrt(face.Rectangle.Area) < 48)
                {
                    continue;
                }
                var pf = _faceClient.FaceList.AddFaceFromStreamAsync(faceListId, new MemoryStream(face.Image),
                    null, null, DetectionModel.Detection02).Result;
                Console.WriteLine($"{face.Id} => {pf.PersistedFaceId}");
            }

            Console.WriteLine("Done");
        }

    }
}
