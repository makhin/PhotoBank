using System.IO;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using Serilog;

namespace PhotoBank.Console
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("consoleapp.log")
                .CreateLogger();

            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetService<App>();
            app?.Run().Wait();
            DisposeServices(serviceProvider);
        }

        private static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            var config = LoadConfiguration();
            string connectionString = config.GetConnectionString("DefaultConnection");

            services.AddDbContext<PhotoBankDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                    builder =>
                    {
                        builder.MigrationsAssembly(typeof(PhotoBankDbContext).GetTypeInfo().Assembly.GetName().Name);
                        builder.UseNetTopologySuite();
                        builder.CommandTimeout(120);
                    });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            RegisterServicesForConsole.Configure(services, config);

            services.AddSingleton(config);
            services.AddTransient<App>();

            services.AddAutoMapper(typeof(MappingProfile));
            services.AddLogging(configure => configure.AddSerilog());

            return services;
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        private static void DisposeServices(IDisposable serviceProvider)
        {
            serviceProvider?.Dispose();
        }
    }
}
