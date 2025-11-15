using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using Serilog;
using System.CommandLine;

namespace PhotoBank.Console
{
    using System;
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
    }
}
