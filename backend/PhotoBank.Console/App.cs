using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoBank.Console
{
    using PhotoBank.Services.Recognition;
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _storages;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IRecognitionService _recognitionService;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> storages, ILogger<App> logger, ISyncService syncService, IRecognitionService recognitionService)
        {
            _photoProcessor = photoProcessor;
            _storages = storages;
            _logger = logger;
            _syncService = syncService;
            _recognitionService = recognitionService;
        }

        public async Task Run()
        {
            await _recognitionService.RegisterPersonsAsync();
            //var storage = await _storages.GetAsync(7);
            //await AddFilesAsync(storage);
        }

        private async Task AddFilesAsync(Storage storage)
        {
            var files = await _syncService.SyncStorage(storage);
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
