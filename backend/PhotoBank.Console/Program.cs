using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using Serilog;
using System.CommandLine;

namespace PhotoBank.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
                System.Console.WriteLine("\nOperation cancelled by user.");
                return 130; // Standard exit code for SIGINT
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Fatal error: {ex.Message}");
                System.Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static RootCommand BuildCommandLine(string[] args)
        {
            var storageOption = new Option<int?>(
                aliases: new[] { "--storage", "-s" },
                description: "Storage ID to process files from");

            var noRegisterOption = new Option<bool>(
                aliases: new[] { "--no-register" },
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

            // Add delete subcommand
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

                System.Console.WriteLine("Starting face embeddings migration...");
                System.Console.WriteLine("This will process all faces without embeddings and extract embeddings from InsightFace API.");
                System.Console.WriteLine();

                var recognitionService = host.Services.GetRequiredService<PhotoBank.Services.Recognition.IRecognitionService>();
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                await recognitionService.MigrateEmbeddingsAsync(lifetime.ApplicationStopping);

                System.Console.WriteLine();
                System.Console.WriteLine("Migration completed successfully!");

                await host.StopAsync();
                return 0;
            }
            catch (OperationCanceledException)
            {
                System.Console.WriteLine("\nMigration cancelled by user.");
                return 130;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Migration error: {ex.Message}");
                Log.Fatal(ex, "Migration failed");
                return 1;
            }
            finally
            {
                host?.Dispose();
                Log.CloseAndFlush();
            }
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
                System.Console.Error.WriteLine($"Configuration error: {ex.Message}");
                return 2;
            }
            catch (OperationCanceledException)
            {
                // Let cancellation bubble up to Main for proper exit code 130
                throw;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Application error: {ex.Message}");
                Log.Fatal(ex, "Application terminated unexpectedly");
                return 1;
            }
            finally
            {
                host?.Dispose();
                Log.CloseAndFlush();
            }
        }

        private static Command BuildReEnrichCommand(string[] args)
        {
            var command = new Command("re-enrich", "Re-run enrichers on already processed photos");

            var storageOption = new Option<int?>(
                aliases: new[] { "--storage", "-s" },
                description: "Storage ID to filter photos");

            var photoIdsOption = new Option<int[]>(
                aliases: new[] { "--photo-ids", "-p" },
                description: "Specific photo IDs to re-enrich (comma-separated)")
            { AllowMultipleArgumentsPerToken = true };

            var enrichersOption = new Option<string[]>(
                aliases: new[] { "--enrichers", "-e" },
                description: "Enricher names to run (e.g., MetadataEnricher, CaptionEnricher). If not specified, runs all active enrichers.")
            { AllowMultipleArgumentsPerToken = true };

            var missingOnlyOption = new Option<bool>(
                aliases: new[] { "--missing-only", "-m" },
                description: "Only apply enrichers that haven't been run yet",
                getDefaultValue: () => false);

            var limitOption = new Option<int?>(
                aliases: new[] { "--limit", "-l" },
                description: "Maximum number of photos to process");

            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
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
                    var activeEnricherProvider = initScope.ServiceProvider.GetRequiredService<PhotoBank.Services.Enrichment.IActiveEnricherProvider>();
                    var enricherRepository = initScope.ServiceProvider.GetRequiredService<PhotoBank.Repositories.IRepository<PhotoBank.DbContext.Models.Enricher>>();
                    var context = initScope.ServiceProvider.GetRequiredService<PhotoBank.DbContext.DbContext.PhotoBankDbContext>();

                    // Resolve enricher types
                    if (enricherNames != null && enricherNames.Length > 0)
                    {
                        enricherTypes = ResolveEnricherTypes(enricherNames, initScope.ServiceProvider);
                        System.Console.WriteLine($"Enrichers to run: {string.Join(", ", enricherTypes.Select(t => t.Name))}");
                    }
                    else
                    {
                        enricherTypes = activeEnricherProvider.GetActiveEnricherTypes(enricherRepository);
                        System.Console.WriteLine($"Using all active enrichers: {string.Join(", ", enricherTypes.Select(t => t.Name))}");
                    }

                    if (!enricherTypes.Any())
                    {
                        System.Console.WriteLine("No enrichers to run.");
                        return 0;
                    }

                    // Build photo query
                    var query = context.Photos.AsQueryable();

                    if (photoIds != null && photoIds.Length > 0)
                    {
                        query = query.Where(p => photoIds.Contains(p.Id));
                        System.Console.WriteLine($"Filtering by photo IDs: {string.Join(", ", photoIds)}");
                    }
                    else if (storageId.HasValue)
                    {
                        query = query.Where(p => p.StorageId == storageId.Value);
                        System.Console.WriteLine($"Filtering by storage ID: {storageId}");
                    }

                    if (limit.HasValue)
                    {
                        query = query.Take(limit.Value);
                        System.Console.WriteLine($"Limiting to {limit} photos");
                    }

                    targetPhotoIds = await query.Select(p => p.Id).ToListAsync(ct);
                    System.Console.WriteLine($"Found {targetPhotoIds.Count} photos to process");
                }

                if (dryRun)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("=== DRY RUN - No changes will be made ===");
                    System.Console.WriteLine($"Would process {targetPhotoIds.Count} photos");
                    System.Console.WriteLine($"Mode: {(missingOnly ? "Missing enrichers only" : "Force re-run all specified enrichers")}");
                    System.Console.WriteLine($"Enrichers: {string.Join(", ", enricherTypes.Select(t => t.Name))}");
                    return 0;
                }

                if (targetPhotoIds.Count == 0)
                {
                    System.Console.WriteLine("No photos to process.");
                    return 0;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Starting re-enrichment ({(missingOnly ? "missing only" : "force re-run")})...");
                System.Console.WriteLine();

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
                    var reEnrichService = photoScope.ServiceProvider.GetRequiredService<PhotoBank.Services.Enrichment.IReEnrichmentService>();

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
                            System.Console.WriteLine($"{progress} Photo {photoId}: OK");
                        }
                        else
                        {
                            skipped++;
                            System.Console.WriteLine($"{progress} Photo {photoId}: Skipped (not found, no files, or already complete)");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        System.Console.WriteLine($"{progress} Photo {photoId}: FAILED - {ex.Message}");
                    }

                    // Progress summary every 100 photos
                    if (processed % 100 == 0)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        var rate = processed / elapsed.TotalSeconds;
                        var remaining = TimeSpan.FromSeconds((targetPhotoIds.Count - processed) / rate);
                        System.Console.WriteLine($"  Progress: {processed}/{targetPhotoIds.Count} ({rate:F1}/sec, ETA: {remaining:hh\\:mm\\:ss})");
                    }
                }

                var totalElapsed = DateTime.UtcNow - startTime;
                System.Console.WriteLine();
                System.Console.WriteLine("=== Re-enrichment complete ===");
                System.Console.WriteLine($"Total: {processed} | Successful: {successful} | Skipped: {skipped} | Failed: {failed}");
                System.Console.WriteLine($"Time: {totalElapsed:hh\\:mm\\:ss}");

                await host.StopAsync();
                return failed > 0 ? 1 : 0;
            }
            catch (OperationCanceledException)
            {
                System.Console.WriteLine("\nRe-enrichment cancelled by user.");
                return 130;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Re-enrichment error: {ex.Message}");
                Log.Fatal(ex, "Re-enrichment failed");
                return 1;
            }
            finally
            {
                host?.Dispose();
                Log.CloseAndFlush();
            }
        }

        private static IReadOnlyCollection<Type> ResolveEnricherTypes(string[] enricherNames, IServiceProvider serviceProvider)
        {
            var enricherAssembly = typeof(PhotoBank.Services.Enrichers.IEnricher).Assembly;
            var allEnricherTypes = enricherAssembly.GetTypes()
                .Where(t => typeof(PhotoBank.Services.Enrichers.IEnricher).IsAssignableFrom(t)
                         && !t.IsInterface
                         && !t.IsAbstract)
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

        private static Command BuildDeleteCommand(string[] args)
        {
            var command = new Command("delete", "Delete a photo by ID, including all related records and S3 objects");

            var photoIdOption = new Option<int>(
                aliases: new[] { "--photo-id", "-p" },
                description: "Photo ID to delete")
            { IsRequired = true };

            var confirmOption = new Option<bool>(
                aliases: new[] { "--confirm", "-y" },
                description: "Confirm deletion without prompting",
                getDefaultValue: () => false);

            command.AddOption(photoIdOption);
            command.AddOption(confirmOption);

            command.SetHandler(async (context) =>
            {
                var photoId = context.ParseResult.GetValueForOption(photoIdOption);
                var confirm = context.ParseResult.GetValueForOption(confirmOption);

                var exitCode = await RunDeleteAsync(args, photoId, confirm);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static async Task<int> RunDeleteAsync(string[] args, int photoId, bool autoConfirm)
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
                            .AddPhotobankCore(context.Configuration);
                    })
                    .Build();

                System.Console.WriteLine($"Preparing to delete photo ID: {photoId}");
                System.Console.WriteLine();

                if (!autoConfirm)
                {
                    System.Console.Write("Are you sure you want to delete this photo? This action cannot be undone. (yes/no): ");
                    var confirmation = System.Console.ReadLine()?.Trim().ToLowerInvariant();

                    if (confirmation != "yes" && confirmation != "y")
                    {
                        System.Console.WriteLine("Deletion cancelled.");
                        return 0;
                    }
                    System.Console.WriteLine();
                }

                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                var ct = lifetime.ApplicationStopping;

                using var scope = host.Services.CreateScope();
                var deletionService = scope.ServiceProvider.GetRequiredService<PhotoBank.Services.Photos.IPhotoDeletionService>();

                System.Console.WriteLine("Deleting photo...");
                var result = await deletionService.DeletePhotoAsync(photoId, ct);
                System.Console.WriteLine();

                result.PrintSummary();

                await host.StopAsync();
                return result.Success ? 0 : 1;
            }
            catch (OperationCanceledException)
            {
                System.Console.WriteLine("\nDeletion cancelled by user.");
                return 130;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Deletion error: {ex.Message}");
                Log.Fatal(ex, "Photo deletion failed");
                return 1;
            }
            finally
            {
                host?.Dispose();
                Log.CloseAndFlush();
            }
        }
    }
}
