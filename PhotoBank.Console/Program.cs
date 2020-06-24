using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Repositories;
using PhotoBank.Services;

namespace PhotoBank.Console
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<App>().Run();
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
                    });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            RegisterServices.Configure(services, config);

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddSingleton(config);
            services.AddTransient<App>();

            return services;
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        private static void DisposeServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                return;
            }
            if (serviceProvider is IDisposable)
            {
                serviceProvider.Dispose();
            }
        }
    }
}
