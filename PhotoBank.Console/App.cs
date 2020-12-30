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

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IPhotoService photoService, IFaceService faceService, ILogger<App> logger)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _photoService = photoService;
            _faceService = faceService;
            _logger = logger;
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
            var storage = await _repository.GetAsync(7);

            //await _photoProcessor.AddPhotoAsync(storage, @"\\192.168.1.35\Public\Photo\RX100\DSC00010.ARW");

            const string supportedExtensions = "*.jpg,*.gif,*.png,*.bmp,*.jpe,*.jpeg,*.tiff,*.arw,*.crw";

            var files = Directory.GetFiles(@"\\MYCLOUDEX2ULTRA\Public\Photo\Sorted", "*.*", SearchOption.AllDirectories)
                .Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()))
                .OrderBy(f => new FileInfo(f).DirectoryName)
                .ThenBy(f => Path.GetFileNameWithoutExtension(new FileInfo(f).Name))
                //.Skip(8500)
                //.Take(1000)
                .ToList();

            //Parallel.ForEach(files,
            //    new ParallelOptions { MaxDegreeOfParallelism = 4 }, 
            //    async (file) =>
            //{
            //    await _photoProcessor.AddPhotoAsync(storage, file);
            //    Console.WriteLine($"Processing {file} on thread {Thread.CurrentThread.ManagedThreadId}");
            //});

            var count = 600;

            foreach (var file in files)
            {
                try
                {
                    if (!await _photoProcessor.AddPhotoAsync(storage, file)) continue;
                    if (count-- == 0)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Debug, e, file);
                }
            }

            Console.WriteLine("Done");
        }
    }
}
