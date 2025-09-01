using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using Serilog;

namespace PhotoBank.Console
{
    using System.Threading;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var options = ConsoleOptions.Parse(args);

            using var host = Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .WriteTo.Console()
                    .WriteTo.File("consoleapp.log"))
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
            await app.RunAsync(options, lifetime.ApplicationStopping);
            await host.StopAsync();
            return 0;
        }
    }
}

