using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Moq;
using PhotoBank.Api;

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

        if (_configuration.Count > 0)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_configuration);
            });
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMinioClient>();
            services.AddSingleton(Mock.Of<IMinioClient>());

            _configureServices?.Invoke(services);
        });
    }
}
