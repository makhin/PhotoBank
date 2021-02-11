using System.IO;
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
        private readonly IFaceService _faceService;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IPhotoService _photoService;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IFaceService faceService, ILogger<App> logger, ISyncService syncService, IPhotoService photoService)
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
            await _photoProcessor.AddFacesAsync(await _repository.GetAsync(3));

            //await AddFilesAsync();

            //await _faceService.SyncPersonsAsync();
            //await _faceService.SyncFacesToPersonAsync();
            //await _faceService.GroupIdentifyAsync();

            //await _faceService.AddFacesToLargeFaceListAsync();
            //await _faceService.ListFindSimilarAsync();
        }

        private async Task AddFilesAsync()
        {
            var storage = await _repository.GetAsync(12);

            var files = await _syncService.SyncStorage(storage);

            //var files = Directory.GetFiles(@"\\MYCLOUDEX2ULTRA\Public\Photo\Veronika.disk\foto\potrets\", "O*.*",
            //    SearchOption.AllDirectories);

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
