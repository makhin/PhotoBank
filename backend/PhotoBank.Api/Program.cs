using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using Microsoft.AspNetCore.Identity;
using PhotoBank.Services;
using PhotoBank.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog.Events;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Formatting.Compact;

namespace PhotoBank.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start App!");

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .WriteTo.Debug()
                .WriteTo.File(new RenderedCompactJsonFormatter(), "Logs/log-.json",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            builder.WebHost.UseKestrel();

            builder.WebHost.UseUrls("http://0.0.0.0:5066");

            // Add services to the container.
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException(
                                       "Connection string 'DefaultConnection' not found.");

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

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddDbContext<PhotoBankDbContext>(options =>
            {
                options.UseLoggerFactory(LoggerFactory.Create(loggingBuilder => loggingBuilder.AddDebug()));
                options.UseSqlServer(connectionString,
                    optionsBuilder =>
                    {
                        optionsBuilder.MigrationsAssembly(typeof(PhotoBankDbContext).GetTypeInfo().Assembly.GetName()
                            .Name);
                        optionsBuilder.UseNetTopologySuite();
                        optionsBuilder.CommandTimeout(120);
                    });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            builder.Services.AddDefaultIdentity<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<PhotoBankDbContext>();

            var jwtSection = configuration.GetSection("Jwt");
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

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AllowToSeeAdultContent", policy => {
                    policy.RequireClaim("AllowAdultContent", "True");
                })
                .AddPolicy("AllowToSeeRacyContent", policy => {
                    policy.RequireClaim("AllowRacyContent", "True");
                });
            builder.Services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    var pd = ctx.ProblemDetails;
                    pd.Instance ??= ctx.HttpContext.Request.Path;
                    pd.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
                };

                options.MapToStatusCode<ArgumentException>(StatusCodes.Status400BadRequest);
                options.MapToStatusCode<UnauthorizedAccessException>(StatusCodes.Status401Unauthorized);
                options.MapToStatusCode<KeyNotFoundException>(StatusCodes.Status404NotFound);
                options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

                options.Map<DomainException>((ex, http) => new ProblemDetails
                {
                    Title = "Business rule violated",
                    Detail = ex.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Type = "https://httpstatuses.com/422",
                    Instance = http.Request.Path
                });
            });

            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
                .AddDbContextCheck<PhotoBankDbContext>("Database", tags: new[] { "ready" });
            builder.Services.AddControllers();
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
            });

            RegisterServicesForApi.Configure(builder.Services);
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
//            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseExceptionHandler();
            app.UseSerilogRequestLogging(opts =>
            {
                opts.EnrichDiagnosticContext = (diag, http) =>
                {
                    diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
                    diag.Set("RemoteIp", http.Connection.RemoteIpAddress?.ToString());
                    diag.Set("RequestId", http.TraceIdentifier);
                };
            });
            app.UseCors("AllowAll");
            // Disabled HTTPS redirection to ensure CORS headers are applied
            // correctly during local development when running over HTTP.
            app.UseAuthentication();
            app.UseMiddleware<ImpersonationMiddleware>();
            app.UseAuthorization();

            app.MapHealthChecks("/healthz/live", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live"),
                ResponseWriter = WriteHealthJson
            });
            app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("ready"),
                ResponseWriter = WriteHealthJson
            });
            app.MapControllers();

            Console.WriteLine("Run App!");
            app.Run();
        }

        private static Task WriteHealthJson(HttpContext ctx, HealthReport report)
        {
            ctx.Response.ContentType = "application/json; charset=utf-8";
            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new
                    {
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        durationMs = e.Value.Duration.TotalMilliseconds,
                        error = e.Value.Exception?.Message
                    })
            };
            return ctx.Response.WriteAsJsonAsync(payload);
        }
    }

    public sealed class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
}
