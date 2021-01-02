using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;

namespace PhotoBank.Console
{
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _repository;
        private readonly IPhotoService _photoService;
        private readonly IFaceService _faceService;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IPhotoService photoService, IFaceService faceService, ILogger<App> logger, ISyncService syncService)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _photoService = photoService;
            _faceService = faceService;
            _logger = logger;
            _syncService = syncService;
        }

        public async Task Run()
        {
            await AddFilesAsync();

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

        private async Task AddFilesAsync()
        {
            var storage = await _repository.GetAsync(9);

            var files = await _syncService.SyncStorage(storage);

            //Parallel.ForEach(files,
            //    new ParallelOptions { MaxDegreeOfParallelism = 4 }, 
            //    async (file) =>
            //{
            //    await _photoProcessor.AddPhotoAsync(storage, file);
            //    Console.WriteLine($"Processing {file} on thread {Thread.CurrentThread.ManagedThreadId}");
            //});

            var count = 2000;

            foreach (var file in files)
            {
                try
                {
                    await _photoProcessor.AddPhotoAsync(storage, file);
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Debug, e, file);
                }

                if (count-- == 0)
                {
                    break;
                }

                var savedColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Count = {count}");
                Console.ForegroundColor = savedColor;
            }

            Console.WriteLine("Done");
        }
    }
}
