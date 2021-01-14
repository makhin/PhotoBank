﻿using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;

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

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IFaceService faceService, ILogger<App> logger, ISyncService syncService)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _faceService = faceService;
            _logger = logger;
            _syncService = syncService;
        }

        public async Task Run()
        {
        }

        private async Task AddFilesAsync()
        {
            var storage = await _repository.GetAsync(9);

            var files = await _syncService.SyncStorage(storage);
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
