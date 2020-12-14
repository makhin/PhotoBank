using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using PhotoBank.Services;

namespace PhotoBank.WindowsForms
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = ConfigureServices();

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                var form1 = serviceProvider.GetRequiredService<MainForm>();
                Application.Run(form1);
            }
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

            RegisterServicesForApi.Configure(services, config);

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddSingleton(config);
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddScoped<MainForm>();

            return services;
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }
    }
}
