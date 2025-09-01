using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using Microsoft.AspNetCore.Identity;
using PhotoBank.Services;
using PhotoBank.Services.FaceRecognition;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using HealthChecks.UI.Client;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text.Json.Serialization;
using PhotoBank.Api.Swagger;
using PhotoBank.Api.Validators;

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

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            builder.Services.AddPhotobankDbContext(builder.Configuration, usePool: true);

            builder.Services.AddDefaultIdentity<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<PhotoBankDbContext>();

            var jwtSection = builder.Configuration.GetSection("Jwt");
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
                };
            });


            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            builder.Services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            builder.Services.AddProblemDetails();

            builder.Services.AddHealthChecks()
                .AddSqlServer(
                    connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
                    name: "sql",
                    tags: new[] { "ready" },
                    failureStatus: HealthStatus.Unhealthy);
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
                });

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                    new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.CustomOperationIds(apiDesc =>
                {
                    if (apiDesc.ActionDescriptor is ControllerActionDescriptor descriptor)
                    {
                        var controllerName = descriptor.ControllerName;
                        var actionName = descriptor.ActionName;
                        return $"{controllerName}_{actionName}";
                    }

                    return null;
                });
                c.DocumentFilter<ServersDocumentFilter>();
            });

            builder.Services
                .AddPhotobankCore(builder.Configuration)
                .AddPhotobankApi();
            var app = builder.Build();

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
                        Instance = context.Request.Path
                    };
                    problem.Extensions["traceId"] = context.TraceIdentifier;

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
            app.UseCors("AllowAll");
            app.UseRouting();
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
