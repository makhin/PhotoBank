using System.Collections.Generic;
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
        private readonly IFaceServiceAws _faceService;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IPhotoService _photoService;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IFaceServiceAws faceService, ILogger<App> logger, ISyncService syncService, IPhotoService photoService)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _faceService = faceService;
            _logger = logger;
            _syncService = syncService;
            _photoService = photoService;
        }

        public async Task Run()
        {
            //await _faceService.SyncPersonsAsync();
            //await _faceService.SyncFacesToPersonAsync();
            //await AddFilesAsync();

            var storage = await _repository.GetAsync(8);
            await _photoProcessor.UpdatePhotosAsync(storage);

            //var storage = await _repository.GetAsync(7);
            //await _photoProcessor.UpdateTakenDateAsync(storage);

            //await _faceService.GroupIdentifyAsync();
            //await _faceService.AddFacesToLargeFaceListAsync();
            //await _faceService.ListFindSimilarAsync();
        }

        private async Task AddFilesAsync()
        {
            var storage = await _repository.GetAsync(13);

//            var files = await _syncService.SyncStorage(storage);

            var files = Directory.GetFiles(@"\\MYCLOUDEX2ULTRA\MobileUploads\Xiaomi 21051182G Camera Backup\", "*.jpg", SearchOption.AllDirectories)
                .ToList();

            var enumerable = files.ToList();

            var count = enumerable.Count;

            foreach (var file in enumerable)
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
