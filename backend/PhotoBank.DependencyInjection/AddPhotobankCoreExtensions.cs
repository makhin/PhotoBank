using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Search;
using PhotoBank.Services.Translator;
using Polly;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankCore(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddMemoryCache();
        services.AddSingleton<IMinioClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey)
                .Build();
        });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IFaceStorageService, FaceStorageService>();
        services.AddScoped<MinioObjectService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddPhotoEvents();
        if (configuration != null)
        {
            services.AddOptions<TranslatorOptions>().Bind(configuration.GetSection("Translator"));
            services.AddOptions<MinioOptions>().Bind(configuration.GetSection("Minio"));
        }
        else
        {
            services.AddOptions<TranslatorOptions>();
            services.AddOptions<MinioOptions>();
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
}
