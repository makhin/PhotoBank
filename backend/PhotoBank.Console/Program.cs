using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoBank.DependencyInjection;
using PhotoBank.Services;
using Serilog;
using System.CommandLine;

namespace PhotoBank.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var rootCommand = BuildCommandLine(args);
                return await rootCommand.InvokeAsync(args);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOperation cancelled by user.");
                return 130; // Standard exit code for SIGINT
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
                await Console.Error.WriteLineAsync(ex.StackTrace);
                return 1;
            }
        }

        private static RootCommand BuildCommandLine(string[] args)
        {
            var storageOption = new Option<int?>(
                aliases: ["--storage", "-s"],
                description: "Storage ID to process files from");

            var noRegisterOption = new Option<bool>(
                aliases: ["--no-register"],
                description: "Skip person registration step",
                getDefaultValue: () => false);

            var rootCommand = new RootCommand("PhotoBank Console - Batch photo processing tool")
            {
                storageOption,
                noRegisterOption
            };

            // Add migrate-embeddings subcommand
            var migrateCommand = new Command("migrate-embeddings", "Migrate face embeddings for all faces without embeddings");
            migrateCommand.SetHandler(async () =>
            {
                var exitCode = await RunMigrationAsync(args);
                Environment.Exit(exitCode);
            });
            rootCommand.AddCommand(migrateCommand);

            // Add re-enrich subcommand
            var reEnrichCommand = BuildReEnrichCommand(args);
            rootCommand.AddCommand(reEnrichCommand);

            // Add delete-photos subcommand
            var deleteCommand = BuildDeleteCommand(args);
            rootCommand.AddCommand(deleteCommand);

            // Allow unmatched tokens (e.g., --environment, --logging:*) to pass through to the host
            rootCommand.TreatUnmatchedTokensAsErrors = false;

            rootCommand.SetHandler(async (context) =>
            {
                var storageId = context.ParseResult.GetValueForOption(storageOption);
                var noRegister = context.ParseResult.GetValueForOption(noRegisterOption);
                var registerPersons = !noRegister;
                var exitCode = await RunApplicationAsync(registerPersons, storageId, args);
                context.ExitCode = exitCode;
            });

            return rootCommand;
        }

        private static async Task<int> RunMigrationAsync(string[] args)
        {
            IHost? host = null;
            try
            {
                host = Host.CreateDefaultBuilder(args)
                    .UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console()
                        .WriteTo.File("logs/photobank-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7))
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddPhotobankDbContext(context.Configuration, usePool: false)
                            .AddPhotobankCore(context.Configuration)
                            .AddPhotobankConsole(context.Configuration);
                    })
                    .Build();

                Console.WriteLine("Starting face embeddings migration...");
                Console.WriteLine("This will process all faces without embeddings and extract embeddings from InsightFace API.");
                Console.WriteLine();

                var recognitionService = host.Services.GetRequiredService<Services.Recognition.IRecognitionService>();
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                await recognitionService.MigrateEmbeddingsAsync(lifetime.ApplicationStopping);

                Console.WriteLine();
                Console.WriteLine("Migration completed successfully!");

                await host.StopAsync();
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nMigration cancelled by user.");
                return 130;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Migration error: {ex.Message}");
                Log.Fatal(ex, "Migration failed");
                return 1;
            }
            finally
            {
                host?.Dispose();
                await Log.CloseAndFlushAsync();
            }
        }

        private static Command BuildDeleteCommand(string[] args)
        {
            var command = new Command("delete-photos", "Delete photos and related S3 objects");

            var photoIdOption = new Option<int?>(
                aliases: ["--photo-id", "-p"],
                description: "Delete a specific photo by ID");

            var lastOption = new Option<int?>(
                aliases: ["--last", "-l"],
                description: "Delete the last N photos ordered by ID");

            command.AddOption(photoIdOption);
            command.AddOption(lastOption);

            command.SetHandler(async context =>
            {
                var photoId = context.ParseResult.GetValueForOption(photoIdOption);
                var lastCount = context.ParseResult.GetValueForOption(lastOption);

                if (!photoId.HasValue && !lastCount.HasValue)
                {
                    await Console.Error.WriteLineAsync("Specify --photo-id to delete a photo or --last to delete the latest photos.");
                    context.ExitCode = 1;
                    return;
                }

                var exitCode = await RunDeleteAsync(args, photoId, lastCount);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static async Task<int> RunApplicationAsync(bool registerPersons, int? storageId, string[] args)
        {
            IHost? host = null;
            try
            {
                host = Host.CreateDefaultBuilder(args)
                    .UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console()
                        .WriteTo.File("logs/photobank-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7))
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddPhotobankDbContext(context.Configuration, usePool: false)
                            .AddPhotobankCore(context.Configuration)
                            .AddPhotobankConsole(context.Configuration)
                            .AddSingleton<App>();
                    })
                    .Build();

                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                var app = host.Services.GetRequiredService<App>();

                var result = await app.RunAsync(registerPersons, storageId, lifetime.ApplicationStopping);

                await host.StopAsync();
                return result;
            }
            catch (InvalidOperationException ex)
            {
                await Console.Error.WriteLineAsync($"Configuration error: {ex.Message}");
                return 2;
            }
            catch (OperationCanceledException)
            {
                // Let cancellation bubble up to Main for proper exit code 130
                throw;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Application error: {ex.Message}");
                Log.Fatal(ex, "Application terminated unexpectedly");
                return 1;
            }
            finally
            {
                host?.Dispose();
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task<int> RunDeleteAsync(string[] args, int? photoId, int? lastCount)
        {
            IHost? host = null;
            try
            {
                host = Host.CreateDefaultBuilder(args)
                    .UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console()
                        .WriteTo.File("logs/photobank-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7))
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddPhotobankDbContext(context.Configuration, usePool: false)
                            .AddPhotobankCore(context.Configuration)
                            .AddPhotobankConsole(context.Configuration);
                    })
                    .Build();

                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                var deletionService = host.Services.GetRequiredService<IPhotoDeletionService>();

                if (photoId.HasValue)
                {
                    var deleted = await deletionService.DeletePhotoAsync(photoId.Value, lifetime.ApplicationStopping);
                    Console.WriteLine(deleted
                        ? $"Deleted photo {photoId.Value}."
                        : $"Photo {photoId.Value} not found.");
                }
                else if (lastCount.HasValue)
                {
                    var deleted = await deletionService.DeleteLastPhotosAsync(lastCount.Value, lifetime.ApplicationStopping);
                    Console.WriteLine($"Deleted {deleted} photo(s) ordered by newest IDs.");
                }

                await host.StopAsync();
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nDeletion cancelled by user.");
                return 130;
            }
            catch (BatchPhotoDeletionException ex)
            {
                await Console.Error.WriteLineAsync($"Deletion error: {ex.Message}");
                if (ex.FailedPhotoIds.Count > 0)
                {
                    await Console.Error.WriteLineAsync($"Failed photo IDs: {string.Join(", ", ex.FailedPhotoIds)}");
                }

                Log.Fatal(ex, "Batch photo deletion failed");
                return 1;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Deletion error: {ex.Message}");
                Log.Fatal(ex, "Photo deletion failed");
                return 1;
            }
            finally
            {
                host?.Dispose();
                await Log.CloseAndFlushAsync();
            }
        }

        private static Command BuildReEnrichCommand(string[] args)
        {
            var command = new Command("re-enrich", "Re-run enrichers on already processed photos");

            var storageOption = new Option<int?>(
                aliases: ["--storage", "-s"],
                description: "Storage ID to filter photos");

            var photoIdsOption = new Option<int[]>(
                aliases: ["--photo-ids", "-p"],
                description: "Specific photo IDs to re-enrich (comma-separated)")
            { AllowMultipleArgumentsPerToken = true };

            var enrichersOption = new Option<string[]>(
                aliases: ["--enrichers", "-e"],
                description: "Enricher names to run (e.g., MetadataEnricher, CaptionEnricher). If not specified, runs all active enrichers.")
            { AllowMultipleArgumentsPerToken = true };

            var missingOnlyOption = new Option<bool>(
                aliases: ["--missing-only", "-m"],
                description: "Only apply enrichers that haven't been run yet",
                getDefaultValue: () => false);

            var limitOption = new Option<int?>(
                aliases: ["--limit", "-l"],
                description: "Maximum number of photos to process");

            var dryRunOption = new Option<bool>(
                aliases: ["--dry-run"],
                description: "Show what would be processed without making changes",
                getDefaultValue: () => false);

            command.AddOption(storageOption);
            command.AddOption(photoIdsOption);
            command.AddOption(enrichersOption);
            command.AddOption(missingOnlyOption);
            command.AddOption(limitOption);
            command.AddOption(dryRunOption);

            command.SetHandler(async (context) =>
            {
                var storageId = context.ParseResult.GetValueForOption(storageOption);
                var photoIds = context.ParseResult.GetValueForOption(photoIdsOption);
                var enricherNames = context.ParseResult.GetValueForOption(enrichersOption);
                var missingOnly = context.ParseResult.GetValueForOption(missingOnlyOption);
                var limit = context.ParseResult.GetValueForOption(limitOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                var exitCode = await RunReEnrichAsync(
                    args, storageId, photoIds, enricherNames, missingOnly, limit, dryRun);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static async Task<int> RunReEnrichAsync(
            string[] args,
            int? storageId,
            int[]? photoIds,
            string[]? enricherNames,
            bool missingOnly,
            int? limit,
            bool dryRun)
        {
            IHost? host = null;
            try
            {
                host = Host.CreateDefaultBuilder(args)
                    .UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console()
                        .WriteTo.File("logs/photobank-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7))
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddPhotobankDbContext(context.Configuration, usePool: false)
                            .AddPhotobankCore(context.Configuration)
                            .AddPhotobankConsole(context.Configuration);
                    })
                    .Build();

                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                var ct = lifetime.ApplicationStopping;
                var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();

                // Resolve enricher types and get photo IDs using a temporary scope
                IReadOnlyCollection<Type> enricherTypes;
                List<int> targetPhotoIds;

                using (var initScope = scopeFactory.CreateScope())
                {
                    var activeEnricherProvider = initScope.ServiceProvider.GetRequiredService<Services.Enrichment.IActiveEnricherProvider>();
                    var enricherRepository = initScope.ServiceProvider.GetRequiredService<Repositories.IRepository<DbContext.Models.Enricher>>();
                    var context = initScope.ServiceProvider.GetRequiredService<PhotoBank.DbContext.DbContext.PhotoBankDbContext>();

                    // Resolve enricher types
                    if (enricherNames != null && enricherNames.Length > 0)
                    {
                        enricherTypes = ResolveEnricherTypes(enricherNames, initScope.ServiceProvider);
                        Console.WriteLine($"Enrichers to run: {string.Join(", ", enricherTypes.Select(t => t.Name))}");
                    }
                    else
                    {
                        enricherTypes = activeEnricherProvider.GetActiveEnricherTypes(enricherRepository);
                        Console.WriteLine($"Using all active enrichers: {string.Join(", ", enricherTypes.Select(t => t.Name))}");
                    }

                    if (!enricherTypes.Any())
                    {
                        Console.WriteLine("No enrichers to run.");
                        return 0;
                    }

                    // Build photo query
                    var query = context.Photos.AsQueryable();

                    if (photoIds != null && photoIds.Length > 0)
                    {
                        query = query.Where(p => photoIds.Contains(p.Id));
                        Console.WriteLine($"Filtering by photo IDs: {string.Join(", ", photoIds)}");
                    }
                    else if (storageId.HasValue)
                    {
                        query = query.Where(p => p.StorageId == storageId.Value);
                        Console.WriteLine($"Filtering by storage ID: {storageId}");
                    }

                    if (limit.HasValue)
                    {
                        query = query.Take(limit.Value);
                        Console.WriteLine($"Limiting to {limit} photos");
                    }

                    targetPhotoIds = await query.Select(p => p.Id).ToListAsync(ct);
                    Console.WriteLine($"Found {targetPhotoIds.Count} photos to process");
                }

                if (dryRun)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== DRY RUN - No changes will be made ===");
                    Console.WriteLine($"Would process {targetPhotoIds.Count} photos");
                    Console.WriteLine($"Mode: {(missingOnly ? "Missing enrichers only" : "Force re-run all specified enrichers")}");
                    Console.WriteLine($"Enrichers: {string.Join(", ", enricherTypes.Select(t => t.Name))}");
                    return 0;
                }

                if (targetPhotoIds.Count == 0)
                {
                    Console.WriteLine("No photos to process.");
                    return 0;
                }

                Console.WriteLine();
                Console.WriteLine($"Starting re-enrichment ({(missingOnly ? "missing only" : "force re-run")})...");
                Console.WriteLine();

                var processed = 0;
                var successful = 0;
                var failed = 0;
                var skipped = 0;
                var startTime = DateTime.UtcNow;

                foreach (var photoId in targetPhotoIds)
                {
                    ct.ThrowIfCancellationRequested();

                    processed++;
                    var progress = $"[{processed}/{targetPhotoIds.Count}]";

                    // Create a new scope for each photo to keep memory bounded
                    using var photoScope = scopeFactory.CreateScope();
                    var reEnrichService = photoScope.ServiceProvider.GetRequiredService<Services.Enrichment.IReEnrichmentService>();

                    try
                    {
                        bool result;
                        if (missingOnly)
                        {
                            result = await reEnrichService.ReEnrichMissingAsync(photoId, ct);
                        }
                        else
                        {
                            result = await reEnrichService.ReEnrichPhotoAsync(photoId, enricherTypes, ct);
                        }

                        if (result)
                        {
                            successful++;
                            Console.WriteLine($"{progress} Photo {photoId}: OK");
                        }
                        else
                        {
                            skipped++;
                            Console.WriteLine($"{progress} Photo {photoId}: Skipped (not found, no files, or already complete)");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Console.WriteLine($"{progress} Photo {photoId}: FAILED - {ex.Message}");
                    }

                    // Progress summary every 100 photos
                    if (processed % 100 == 0)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        var rate = processed / elapsed.TotalSeconds;
                        var remaining = TimeSpan.FromSeconds((targetPhotoIds.Count - processed) / rate);
                        Console.WriteLine($"  Progress: {processed}/{targetPhotoIds.Count} ({rate:F1}/sec, ETA: {remaining:hh\\:mm\\:ss})");
                    }
                }

                var totalElapsed = DateTime.UtcNow - startTime;
                Console.WriteLine();
                Console.WriteLine("=== Re-enrichment complete ===");
                Console.WriteLine($"Total: {processed} | Successful: {successful} | Skipped: {skipped} | Failed: {failed}");
                Console.WriteLine($@"Time: {totalElapsed:hh\:mm\:ss}");

                await host.StopAsync(ct);
                return failed > 0 ? 1 : 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nRe-enrichment cancelled by user.");
                return 130;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Re-enrichment error: {ex.Message}");
                Log.Fatal(ex, "Re-enrichment failed");
                return 1;
            }
            finally
            {
                host?.Dispose();
                await Log.CloseAndFlushAsync();
            }
        }

        private static IReadOnlyCollection<Type> ResolveEnricherTypes(string[] enricherNames, IServiceProvider serviceProvider)
        {
            var enricherAssembly = typeof(Services.Enrichers.IEnricher).Assembly;
            var allEnricherTypes = enricherAssembly.GetTypes()
                .Where(t => typeof(Services.Enrichers.IEnricher).IsAssignableFrom(t)
                            && t is {IsInterface: false, IsAbstract: false})
                .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            // Get list of registered enrichers for better error messages
            var registeredEnrichers = allEnricherTypes.Values
                .Where(t => serviceProvider.GetService(t) != null)
                .Select(t => t.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new List<Type>();
            foreach (var name in enricherNames)
            {
                // Try exact match first, then try with "Enricher" suffix
                var searchName = name;
                if (!allEnricherTypes.TryGetValue(searchName, out var type))
                {
                    searchName = name + "Enricher";
                    if (!allEnricherTypes.TryGetValue(searchName, out type))
                    {
                        throw new ArgumentException(
                            $"Unknown enricher: '{name}'. Available enrichers: {string.Join(", ", registeredEnrichers.OrderBy(k => k))}");
                    }
                }

                // Validate that the enricher is registered in DI
                if (!registeredEnrichers.Contains(type.Name))
                {
                    throw new ArgumentException(
                        $"Enricher '{type.Name}' exists but is not registered (possibly disabled in configuration). " +
                        $"Available enrichers: {string.Join(", ", registeredEnrichers.OrderBy(k => k))}");
                }

                result.Add(type);
            }

            return result;
        }
    }
}
