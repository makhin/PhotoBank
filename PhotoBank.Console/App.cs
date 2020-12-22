using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using System;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using PhotoBank.Dto;

namespace PhotoBank.Console
{
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _repository;
        private readonly IPhotoService _photoService;
        private readonly IFaceService _faceService;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IPhotoService photoService, IFaceService faceService)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _photoService = photoService;
            _faceService = faceService;
        }

        public void Run()
        {
            //AddFiles();
            //_faceService.AddFacesToList().Wait();
            //_faceService.FindSimilarFaces();

            //_faceService.GetOrCreatePersonGroupAsync().Wait();
            //_faceService.SyncPersonsAsync().Wait();
            //_faceService.SyncFacesToPersonAsync().Wait();
            //_faceService.FindFaceAsync().Wait();
            //_faceService.FindSimilarFaces().Wait();
            //_faceService.FindSimilarFacesInList().Wait();

            //_faceService.Test().Wait();
        }


        private void AddFiles()
        {
            var storage = _repository.Get(3);

            var files = Directory.GetFiles(@"\\192.168.1.35\Public\Photo\RX100_2\", "*.*")
                .OrderBy(f => Path.GetFileNameWithoutExtension(new FileInfo(f).Name))
                .ThenBy(f => new FileInfo(f).Length)
                .Skip(739)
                .Take(3000);

            foreach (var file in files)
            {
                Console.WriteLine(file);
                _photoProcessor.AddPhoto(storage, file);
                //Task.Delay(3000).Wait();
            }

            Console.WriteLine("Done");
        }
    }
}
