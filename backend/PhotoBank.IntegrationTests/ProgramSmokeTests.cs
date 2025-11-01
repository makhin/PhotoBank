using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.Api;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class ProgramSmokeTests
{
    private TextWriter? _originalConsoleOut;

    [OneTimeSetUp]
    public void SuppressConsoleNoise()
    {
        _originalConsoleOut = Console.Out;
        Console.SetOut(TextWriter.Null);
    }

    [OneTimeTearDown]
    public void RestoreConsole()
    {
        if (_originalConsoleOut is not null)
        {
            Console.SetOut(_originalConsoleOut);
        }
    }

    [Test]
    public async Task Application_WithDefaultConfiguration_ExposesSwaggerAndHealth()
    {
        using var factory = new SmokeWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var readiness = await client.GetAsync("/api/health");
        readiness.StatusCode.Should().Be(HttpStatusCode.OK);
        readiness.Headers.Should().ContainKey("X-Correlation-Id");

        var swaggerUi = await client.GetAsync("/api/swagger/index.html");
        swaggerUi.StatusCode.Should().Be(HttpStatusCode.OK);

        var swaggerDoc = await client.GetAsync("/api/swagger/v1/swagger.json");
        swaggerDoc.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Application_WithMinimalConfiguration_UsesConfiguredHealthPaths()
    {
        var minimalConfiguration = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=photobank_test;Username=postgres;Password=postgres",
            ["Jwt:Key"] = "super-secret-key-for-tests",
            ["Jwt:Issuer"] = "PhotoBank.Tests",
            ["Jwt:Audience"] = "PhotoBank.Tests",
            ["HealthChecks:ReadinessPath"] = "/ready",
            ["HealthChecks:LivenessPath"] = "/live"
        };

        using var factory = new SmokeWebApplicationFactory(minimalConfiguration);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var readiness = await client.GetAsync("/api/ready");
        readiness.StatusCode.Should().Be(HttpStatusCode.OK);
        readiness.Headers.Should().ContainKey("X-Correlation-Id");

        var liveness = await client.GetAsync("/api/live");
        liveness.StatusCode.Should().Be(HttpStatusCode.OK);
        liveness.Headers.Should().ContainKey("X-Correlation-Id");
    }

    [Test]
    public async Task Application_WhenEndpointThrows_ReturnsProblemDetails()
    {
        using var factory = new SmokeWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var response = await client.GetAsync("/api/test-error");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");

        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("\"title\":\"Bad Request\"");
        payload.Should().Contain("Test error");

        payload.Should().Contain("\"traceId\"");
    }

    private sealed class SmokeWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?>? _configuration;

        public SmokeWebApplicationFactory()
        {
        }

        public SmokeWebApplicationFactory(IReadOnlyDictionary<string, string?> configuration)
        {
            _configuration = configuration;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);

            if (_configuration is not null)
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.Sources.Clear();
                    configBuilder.AddInMemoryCollection(_configuration);
                });
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<PhotoBankDbContext>>();
                services.RemoveAll<PhotoBankDbContext>();
                services.RemoveAll<DbContextOptions<AccessControlDbContext>>();
                services.RemoveAll<AccessControlDbContext>();

                services.AddDbContext<PhotoBankDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"photobank-{Guid.NewGuid():N}");
                });

                services.AddDbContext<AccessControlDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"photobank-access-{Guid.NewGuid():N}");
                });

                services.RemoveAll<IMinioClient>();
                services.AddSingleton(Mock.Of<IMinioClient>());

                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                    options.Registrations.Add(new HealthCheckRegistration(
                        name: "self",
                        factory: _ => new AlwaysHealthyHealthCheck(),
                        failureStatus: HealthStatus.Unhealthy,
                        tags: new[] { "ready" }));
                });

                services.PostConfigure<AuthorizationOptions>(options =>
                {
                    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAssertion(_ => true)
                        .Build();
                });

                services.AddSingleton<IStartupFilter>(new ExceptionEndpointStartupFilter());
            });
        }

        private sealed class AlwaysHealthyHealthCheck : IHealthCheck
        {
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Test health check"));
            }
        }

        private sealed class ExceptionEndpointStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    next(app);

                    app.Map("/test-error", branch =>
                    {
                        branch.Run(_ => throw new InvalidOperationException("Test error"));
                    });
                };
            }
        }
    }
}
