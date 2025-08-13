using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Recognition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Console
{
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _storages;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IRecognitionService _recognitionService;
        private readonly object _progressLock = new();

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> storages, ILogger<App> logger, ISyncService syncService, IRecognitionService recognitionService)
        {
            _photoProcessor = photoProcessor;
            _storages = storages;
            _logger = logger;
            _syncService = syncService;
            _recognitionService = recognitionService;
        }

        public async Task RunAsync(ConsoleOptions options, CancellationToken token)
        {
            if (options.RegisterPersons)
            {
                await _recognitionService.RegisterPersonsAsync();
            }

            if (options.StorageId.HasValue)
            {
                var storage = await _storages.GetAsync(options.StorageId.Value);
                if (storage == null)
                {
                    _logger.LogError("Storage {StorageId} not found", options.StorageId);
                }
                else
                {
                    await AddFilesAsync(storage, token);
                }
            }
        }

        private async Task AddFilesAsync(Storage storage, CancellationToken token)
        {
            var files = await _syncService.SyncStorage(storage);
            var fileList = files.ToList();
            var total = fileList.Count;
            var processed = 0;
            var failed = 0;

            Console.WriteLine($"Processing {total} files...");
            DisplayProgress(0, 0, total);

            await Parallel.ForEachAsync(fileList, token, async (file, ct) =>
            {
                try
                {
                    await _photoProcessor.AddPhotoAsync(storage, file);
                    Interlocked.Increment(ref processed);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error processing {File}", file);
                    Interlocked.Increment(ref failed);
                }

                DisplayProgress(Volatile.Read(ref processed), Volatile.Read(ref failed), total);
            });

            Console.WriteLine();
            _logger.LogInformation("Processed {Processed} files with {Failed} failures", processed, failed);
            Console.WriteLine("Done");
        }

        private void DisplayProgress(int processed, int failed, int total)
        {
            const int width = 40;
            var completed = processed + failed;
            var percent = (double)completed / Math.Max(total, 1);
            var filled = (int)(percent * width);
            var bar = new string('#', filled).PadRight(width);

            lock (_progressLock)
            {
                Console.Write($"\r[{bar}] {completed}/{total} files ({failed} failed)");
            }
        }
    }
}

