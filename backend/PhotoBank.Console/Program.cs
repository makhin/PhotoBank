using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Repositories;
using PhotoBank.Services;
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
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

                    services.AddDbContext<PhotoBankDbContext>(opts =>
                    {
                        opts.UseSqlServer(connectionString, builder =>
                        {
                            builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                            builder.UseNetTopologySuite();
                            builder.CommandTimeout(120);
                        });
                        opts.EnableSensitiveDataLogging();
                        opts.EnableDetailedErrors();
                    });

                    RegisterServicesForConsole.Configure(services, context.Configuration);

                    services.AddSingleton<App>();
                    services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
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

