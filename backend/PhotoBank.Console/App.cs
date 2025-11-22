using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichment;
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
        private readonly IRepository<Enricher> _enricherRepository;
        private readonly IActiveEnricherProvider _activeEnricherProvider;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IRecognitionService _recognitionService;
        private readonly bool _checkDuplicates;
        private readonly object _progressLock = new();

        public App(
            IPhotoProcessor photoProcessor,
            IRepository<Storage> storages,
            IRepository<Enricher> enricherRepository,
            IActiveEnricherProvider activeEnricherProvider,
            ILogger<App> logger,
            ISyncService syncService,
            IRecognitionService recognitionService,
            IConfiguration configuration)
        {
            _photoProcessor = photoProcessor;
            _storages = storages;
            _enricherRepository = enricherRepository;
            _activeEnricherProvider = activeEnricherProvider;
            _logger = logger;
            _syncService = syncService;
            _recognitionService = recognitionService;
            _checkDuplicates = configuration.GetValue("CheckDuplicates", true);
        }

        public async Task<int> RunAsync(bool registerPersons, int? storageId, CancellationToken token)
        {
            try
            {
                if (registerPersons)
                {
                    _logger.LogInformation("Starting person registration...");
                    await _recognitionService.RegisterPersonsAsync();
                    _logger.LogInformation("Person registration completed");
                }

                if (storageId.HasValue)
                {
                    return await ProcessStorageAsync(storageId.Value, token);
                }

                if (!registerPersons)
                {
                    System.Console.WriteLine("No operation specified. Use --storage to process files or enable person registration.");
                    System.Console.WriteLine("Run with --help for usage information.");
                    return 0;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in application");
                return 1;
            }
        }

        private async Task<int> ProcessStorageAsync(int storageId, CancellationToken token)
        {
            _logger.LogInformation("Loading storage {StorageId}...", storageId);

            var storage = await _storages.GetAsync(storageId);

            if (storage == null)
            {
                var errorMessage = $"Storage with ID {storageId} not found";
                _logger.LogError(errorMessage);
                System.Console.Error.WriteLine($"Error: {errorMessage}");
                System.Console.Error.WriteLine("Please verify the storage ID exists in the database.");
                return 3; // Exit code for "not found"
            }

            _logger.LogInformation("Processing storage: {StorageName} (ID: {StorageId})", storage.Name, storage.Id);
            return await AddFilesAsync(storage, token);
        }

        private async Task<int> AddFilesAsync(Storage storage, CancellationToken token)
        {
            try
            {
                var files = await _syncService.SyncStorage(storage);
                var fileList = files.ToList();
                var total = fileList.Count;

                if (total == 0)
                {
                    _logger.LogInformation("No files found in storage {StorageId}", storage.Id);
                    System.Console.WriteLine("No files to process.");
                    return 0;
                }

                var processed = 0;
                var failed = 0;
                var duplicates = 0;

                var activeEnrichers = _activeEnricherProvider.GetActiveEnricherTypes(_enricherRepository);

                System.Console.WriteLine($"Processing {total} files from storage '{storage.Name}'...");
                DisplayProgress(0, 0, 0, total);

                await Parallel.ForEachAsync(fileList, token, async (file, ct) =>
                {
                    try
                    {
                        if (_checkDuplicates && await _photoProcessor.IsDuplicateAsync(storage, file))
                        {
                            Interlocked.Increment(ref duplicates);
                        }
                        else
                        {
                            await _photoProcessor.AddPhotoAsync(storage, file, activeEnrichers);
                            Interlocked.Increment(ref processed);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error processing {File}", file);
                        Interlocked.Increment(ref failed);
                    }

                    DisplayProgress(Volatile.Read(ref processed), Volatile.Read(ref failed), Volatile.Read(ref duplicates), total);
                });

                System.Console.WriteLine();
                _logger.LogInformation("Processing completed: {Processed} processed, {Failed} failed, {Duplicates} duplicates",
                    processed, failed, duplicates);
                System.Console.WriteLine($"Done! Processed: {processed}, Failed: {failed}, Duplicates: {duplicates}");

                // Return non-zero exit code if there were failures
                return failed > 0 ? 4 : 0; // Exit code 4 for "partial failure"
            }
            catch (OperationCanceledException)
            {
                // Let cancellation bubble up to be handled at the top level
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing storage {StorageId}", storage.Id);
                System.Console.Error.WriteLine($"Error processing storage: {ex.Message}");
                return 1;
            }
        }

        private void DisplayProgress(int processed, int failed, int duplicates, int total)
        {
            const int width = 40;
            var completed = processed + failed + duplicates;
            var percent = (double)completed / Math.Max(total, 1);
            var filled = (int)(percent * width);
            var bar = new string('#', filled).PadRight(width);

            lock (_progressLock)
            {
                System.Console.Write($"\r[{bar}] {completed}/{total} files ({failed} failed, {duplicates} duplicates)");
            }
        }
    }
}
