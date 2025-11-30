using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<Storage> _storages;
        private readonly IRepository<Enricher> _enricherRepository;
        private readonly IActiveEnricherProvider _activeEnricherProvider;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IRecognitionService _recognitionService;
        private readonly int _maxDegreeOfParallelism;
        private readonly Lock _progressLock = new();

        public App(
            IServiceProvider serviceProvider,
            IRepository<Storage> storages,
            IRepository<Enricher> enricherRepository,
            IActiveEnricherProvider activeEnricherProvider,
            ILogger<App> logger,
            ISyncService syncService,
            IRecognitionService recognitionService,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _storages = storages;
            _enricherRepository = enricherRepository;
            _activeEnricherProvider = activeEnricherProvider;
            _logger = logger;
            _syncService = syncService;
            _recognitionService = recognitionService;
            _maxDegreeOfParallelism = configuration.GetValue<int?>("Processing:MaxDegreeOfParallelism")
                ?? Environment.ProcessorCount;
        }

        public async Task<int> RunAsync(bool registerPersons, int? storageId, CancellationToken token)
        {
            try
            {
                if (registerPersons)
                {
                    _logger.LogInformation("Starting person registration...");
                    await _recognitionService.RegisterPersonsAsync(token);
                    _logger.LogInformation("Person registration completed");
                }

                if (storageId.HasValue)
                {
                    return await ProcessStorageAsync(storageId.Value, token);
                }

                if (registerPersons) return 0;
                Console.WriteLine("No operation specified. Use --storage to process files or enable person registration.");
                Console.WriteLine("Run with --help for usage information.");
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
                    Console.WriteLine("No files to process.");
                    return 0;
                }

                var processed = 0;
                var failed = 0;
                var duplicates = 0;
                var skipped = 0;

                var activeEnrichers = _activeEnricherProvider.GetActiveEnricherTypes(_enricherRepository);
                var storageId = storage.Id; // Store storage ID to avoid cross-context issues
                var storageName = storage.Name;

                Console.WriteLine($"Processing {total} files from storage '{storageName}'...");
                Console.WriteLine($"Max degree of parallelism: {_maxDegreeOfParallelism}");
                _logger.LogInformation("Using max degree of parallelism: {MaxDegreeOfParallelism}", _maxDegreeOfParallelism);
                DisplayProgress(0, 0, 0, 0, total);

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                    CancellationToken = token
                };

                await Parallel.ForEachAsync(fileList, parallelOptions, async (file, ct) =>
                {
                    // Create a new scope for each file to ensure each parallel operation
                    // gets its own DbContext instance, preventing thread-safety issues
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var photoProcessor = scope.ServiceProvider.GetRequiredService<IPhotoProcessor>();
                    var storageRepository = scope.ServiceProvider.GetRequiredService<IRepository<Storage>>();

                    try
                    {
                        // Load storage in this scope's DbContext to avoid detached entity issues
                        var scopedStorage = await storageRepository.GetAsync(storageId);
                        var (photoId, result, skipReason) = await photoProcessor.AddPhotoAsync(scopedStorage, file, activeEnrichers);

                        switch (result)
                        {
                            case PhotoProcessResult.Added:
                                Interlocked.Increment(ref processed);
                                break;
                            case PhotoProcessResult.Duplicate:
                                Interlocked.Increment(ref duplicates);
                                break;
                            case PhotoProcessResult.Skipped:
                                Interlocked.Increment(ref skipped);
                                _logger.LogInformation("Skipped {File}: {Reason}", file, skipReason);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error processing {File}", file);
                        Interlocked.Increment(ref failed);
                    }

                    DisplayProgress(Volatile.Read(ref processed), Volatile.Read(ref failed), Volatile.Read(ref duplicates), Volatile.Read(ref skipped), total);
                });

                Console.WriteLine();
                _logger.LogInformation("Processing completed: {Processed} processed, {Failed} failed, {Duplicates} duplicates, {Skipped} skipped",
                    processed, failed, duplicates, skipped);
                Console.WriteLine($"Done! Processed: {processed}, Failed: {failed}, Duplicates: {duplicates}, Skipped: {skipped}");

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
                await Console.Error.WriteLineAsync($"Error processing storage: {ex.Message}");
                return 1;
            }
        }

        private void DisplayProgress(int processed, int failed, int duplicates, int skipped, int total)
        {
            const int width = 40;
            var completed = processed + failed + duplicates + skipped;
            var percent = (double)completed / Math.Max(total, 1);
            var filled = (int)(percent * width);
            var bar = new string('#', filled).PadRight(width);

            lock (_progressLock)
            {
                Console.Write($"\r[{bar}] {completed}/{total} files ({failed} failed, {duplicates} duplicates, {skipped} skipped)");
            }
        }
    }
}
