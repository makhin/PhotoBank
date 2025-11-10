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
                var rootCommand = BuildCommandLine();
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

        private static RootCommand BuildCommandLine()
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

            rootCommand.SetHandler(async (storageId, noRegister) =>
            {
                var registerPersons = !noRegister;
                await RunApplicationAsync(registerPersons, storageId);
            }, storageOption, noRegisterOption);

            return rootCommand;
        }

        private static async Task<int> RunApplicationAsync(bool registerPersons, int? storageId)
        {
            IHost? host = null;
            try
            {
                host = Host.CreateDefaultBuilder()
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
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Application error: {ex.Message}");
                Log.Fatal(ex, "Application terminated unexpectedly");
                return 1;
            }
            finally
            {
                host?.Dispose();
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
