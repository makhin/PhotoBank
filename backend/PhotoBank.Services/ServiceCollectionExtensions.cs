using Amazon.Rekognition;
using MediatR;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Minio;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.InsightFaceApiClient;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Events;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.Recognition;
using PhotoBank.Services.Search;
using PhotoBank.Services.Translator;
using Polly;
using System;
using System.Linq;
using System.Text;
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;

namespace PhotoBank.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankCore(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddMemoryCache();
        services.AddSingleton<IMinioClient>(_ => new MinioClient()
            .WithEndpoint("localhost")
            .WithCredentials("", "")
            .Build());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddPhotoEvents();
        if (configuration != null)
        {
            services.AddOptions<TranslatorOptions>().Bind(configuration.GetSection("Translator"));
        }
        else
        {
            services.AddOptions<TranslatorOptions>();
        }
        services.AddHttpClient<ITranslatorService, TranslatorService>()
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * attempt)));
        services.AddScoped<ISearchFilterNormalizer, SearchFilterNormalizer>();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        return services;
    }

    public static IServiceCollection AddPhotobankApi(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IS3ResourceService, S3ResourceService>();
        services.AddTransient<IFaceStorageService, FaceStorageService>();
        services.AddScoped<IEffectiveAccessProvider, EffectiveAccessProvider>();
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.AddDefaultIdentity<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PhotoBankDbContext>();

        if (configuration != null)
        {
            var jwtSection = configuration.GetSection("Jwt");
            services.AddAuthentication(options =>
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
        }

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    public static IServiceCollection AddPhotobankConsole(this IServiceCollection services, IConfiguration configuration)
    {
        const string computerVision = "ComputerVision";
        const string face = "Face";

        services.AddSingleton<IComputerVisionClient, ComputerVisionClient>(provider =>
        {
            var key = configuration.GetSection(computerVision)["Key"];
            var credentials = new ApiKeyServiceClientCredentials(key);
            return new ComputerVisionClient(credentials)
            {
                Endpoint = configuration.GetSection(computerVision)["Endpoint"]
            };
        });

        services.AddSingleton<IFaceClient, FaceClient>(provider =>
        {
            var key = configuration.GetSection(face)["Key"];
            var credentials = new ApiKeyServiceClientCredentials(key);
            return new FaceClient(credentials)
            {
                Endpoint = configuration.GetSection(face)["Endpoint"]
            };
        });

        services.AddSingleton(typeof(AmazonRekognitionClient));
        services.AddTransient<IDependencyExecutor, DependencyExecutor>();
        services.AddTransient<IFaceService, FaceService>();
        services.AddTransient<IFacePreviewService, FacePreviewService>();
        services.AddTransient<IFaceServiceAws, FaceServiceAws>();
        services.AddTransient<IFaceStorageService, FaceStorageService>();
        services.AddTransient<IPhotoProcessor, PhotoProcessor>();
        services.AddTransient<IPhotoService, PhotoService>();
        services.AddSingleton<ICurrentUser, DummyCurrentUser>();
        services.AddTransient<IImageService, ImageService>();
        services.AddTransient<ISyncService, SyncService>();
        services.AddTransient<IEnricher, MetadataEnricher>();
        services.AddTransient<IEnricher, ThumbnailEnricher>();
        services.AddTransient<IEnricher, PreviewEnricher>();
        services.AddTransient<IEnricher, AnalyzeEnricher>();
        services.AddTransient<IEnricher, ColorEnricher>();
        services.AddTransient<IEnricher, CaptionEnricher>();
        services.AddTransient<IEnricher, TagEnricher>();
        services.AddTransient<IEnricher, ObjectPropertyEnricher>();
        services.AddTransient<IEnricher, AdultEnricher>();
        services.AddTransient<IEnricher, FaceEnricher>();
        services.AddTransient<IEnricher, FaceEnricherAws>();
        services.AddTransient<IImageMetadataReaderWrapper, ImageMetadataReaderWrapper>();
        services.AddFaceRecognition(configuration);
        services.AddScoped<UnifiedFaceService>();
        services.AddScoped<IFaceService, FaceService>();
        services.AddSingleton<IInsightFaceApiClient, InsightFaceApiClient.InsightFaceApiClient>();
        services.AddTransient<IRecognitionService, RecognitionService>();
        services.AddSingleton<EnricherResolver>(provider =>
        {
            var enricherTypes = typeof(IEnricher).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IEnricher).IsAssignableFrom(t))
                .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            return repository =>
            {
                return repository.GetAll()
                    .Where(e => e.IsActive)
                    .AsEnumerable()
                    .Select(e =>
                    {
                        if (!enricherTypes.TryGetValue(e.Name, out var type))
                            throw new NotSupportedException($"Enricher '{e.Name}' not found in loaded assemblies.");
                        return (IEnricher)provider.GetRequiredService(type);
                    });
            };
        });
        return services;
    }

    public static IServiceCollection AddPhotobankDbContext(this IServiceCollection services, IConfiguration configuration, bool usePool)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<DbTimingInterceptor>();

        if (usePool)
        {
            services.AddDbContextPool<PhotoBankDbContext>((sp, options) =>
            {
                var env = sp.GetRequiredService<IHostEnvironment>();
                options.AddInterceptors(sp.GetRequiredService<DbTimingInterceptor>());
                options
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .EnableDetailedErrors(env.IsDevelopment())
                    .EnableSensitiveDataLogging(env.IsDevelopment());

                options.UseLoggerFactory(LoggerFactory.Create(b => b.AddDebug()));

                options.UseSqlServer(
                    connectionString,
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                        sql.UseNetTopologySuite();
                        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
                        sql.CommandTimeout(60);
                        sql.MaxBatchSize(128);
                        sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
            });
        }
        else
        {
            services.AddDbContext<PhotoBankDbContext>((sp, options) =>
            {
                var env = sp.GetRequiredService<IHostEnvironment>();
                options.AddInterceptors(sp.GetRequiredService<DbTimingInterceptor>());
                options
                    .EnableDetailedErrors(env.IsDevelopment())
                    .EnableSensitiveDataLogging(env.IsDevelopment());

                options.UseSqlServer(
                    connectionString,
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                        sql.UseNetTopologySuite();
                        sql.CommandTimeout(120);
                    });
            });
        }

        services.AddDbContext<AccessControlDbContext>(opt => { opt.UseSqlServer(connectionString); });

        return services;
    }

    public static IServiceCollection AddPhotoEvents(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PhotoCreated>());
        return services;
    }
}
