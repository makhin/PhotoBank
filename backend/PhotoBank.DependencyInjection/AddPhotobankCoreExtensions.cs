using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Search;
using PhotoBank.Services.Translator;
using Polly;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
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
}
