using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Moq;
using PhotoBank.AccessControl;
using PhotoBank.Api;
using PhotoBank.DbContext.DbContext;
using System;
using System.Collections.Generic;

namespace PhotoBank.IntegrationTests.Infra;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly IReadOnlyDictionary<string, string?> _configuration;
    private readonly Action<IServiceCollection>? _configureServices;

    public ApiWebApplicationFactory(
        IReadOnlyDictionary<string, string?>? configuration = null,
        Action<IServiceCollection>? configureServices = null,
        string environment = "Development")
    {
        _environment = environment;
        _configuration = configuration ?? new Dictionary<string, string?>();
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        // Добавляем тестовую конфигурацию (важно: ключ для строки подключения)
        // Например: { "ConnectionStrings:DefaultConnection": "<conn-string>" }
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(_configuration);
        });

        builder.ConfigureServices((context, services) =>
        {
            // Берём строку подключения из IConfiguration
            var cs =
                context.Configuration.GetConnectionString("DefaultConnection")
                ?? context.Configuration["DefaultConnection"]; // запасной вариант

            // Перерегистрируем опции контекстов
            services.RemoveAll<DbContextOptions<PhotoBankDbContext>>();
            services.RemoveAll<DbContextOptions<AccessControlDbContext>>();

            services.AddDbContext<PhotoBankDbContext>(opt =>
                opt.UseNpgsql(cs, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Photo");
                    npgsql.UseNetTopologySuite();
                }));

            services.AddDbContext<AccessControlDbContext>(opt =>
                opt.UseNpgsql(cs, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.GetName().Name);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Access");
                    npgsql.UseNetTopologySuite();
                }));

            _configureServices?.Invoke(services);
        });
    }
}