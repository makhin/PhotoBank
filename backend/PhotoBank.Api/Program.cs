using Microsoft.AspNetCore.Mvc;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using HealthChecks.UI.Client;
using PhotoBank.Api.Swagger;

namespace PhotoBank.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start App!");

            var builder = WebApplication.CreateBuilder(args);

            // Serilog из конфигурации
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            builder.Host.UseSerilog();

            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls("http://0.0.0.0:5066");

            builder.Services
                .AddPhotobankDbContext(builder.Configuration, usePool: false)
                .AddPhotobankCore(builder.Configuration)
                .AddPhotobankApi(builder.Configuration)
                .AddPhotobankMvc(builder.Configuration)
                .AddPhotobankCors()
                .AddPhotobankSwagger(c =>
                {
                    c.DocumentFilter<ServersDocumentFilter>();
                });
            var app = builder.Build();

            app.UsePathBase("/api");

            // Configure the HTTP request pipeline.
//            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Корреляция по запросу (X-Correlation-Id)
            app.Use(async (ctx, next) =>
            {
                const string header = "X-Correlation-Id";
                if (!ctx.Request.Headers.TryGetValue(header, out var cid) || string.IsNullOrWhiteSpace(cid))
                {
                    cid = ctx.TraceIdentifier;
                    ctx.Response.Headers[header] = cid;
                }
                Serilog.Context.LogContext.PushProperty("CorrelationId", cid.ToString());
                await next();
            });

            // Глобальный обработчик исключений → RFC7807
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var feature = context.Features.Get<IExceptionHandlerFeature>();
                    var ex = feature?.Error;

                    var (status, title, type) = ex switch
                    {
                        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "https://httpstatuses.io/401"),
                        ArgumentException or InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request", "https://httpstatuses.io/400"),
                        Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict", "https://httpstatuses.io/409"),
                        Microsoft.EntityFrameworkCore.DbUpdateException => (StatusCodes.Status409Conflict, "Database update failed", "https://httpstatuses.io/409"),
                        _ => (StatusCodes.Status500InternalServerError, "Server Error", "https://httpstatuses.io/500")
                    };

                    var problem = new ProblemDetails
                    {
                        Status = status,
                        Title = title,
                        Type = type,
                        Detail = app.Environment.IsDevelopment() ? ex?.Message : "An error occurred.",
                        Instance = context.Request.Path,
                        Extensions =
                        {
                            ["traceId"] = context.TraceIdentifier
                        }
                    };

                    context.Response.ContentType = "application/problem+json";
                    context.Response.StatusCode = status;
                    await context.Response.WriteAsJsonAsync(problem);
                });
            });

            app.UseSerilogRequestLogging(opts =>
            {
                opts.EnrichDiagnosticContext = (diag, http) =>
                {
                    diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
                    diag.Set("RemoteIp", http.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
                    diag.Set("RequestId", http.TraceIdentifier);
                };
            });

            app.UseRouting();
            app.UseCors("AllowAll");
            // Disabled HTTPS redirection to ensure CORS headers are applied
            // correctly during local development when running over HTTP.
            app.UseAuthentication();
            // имперсонификация удалена
            app.UseAuthorization();

            app.MapControllers();

            // Health endpoints
            app.MapHealthChecks(
                builder.Configuration["HealthChecks:ReadinessPath"] ?? "/health",
                new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            app.MapHealthChecks(
                builder.Configuration["HealthChecks:LivenessPath"] ?? "/liveness",
                new HealthCheckOptions
                {
                    Predicate = _ => false // только инфраструктура процесса
                });

            Console.WriteLine("Run App!");
            app.Run();
        }

    }

    public sealed class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
}
