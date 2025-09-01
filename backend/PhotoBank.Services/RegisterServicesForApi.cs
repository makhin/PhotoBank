using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;
using PhotoBank.AccessControl;
using PhotoBank.Services.Translator;
using PhotoBank.Services.Search;
using Polly;

namespace PhotoBank.Services
{
    public static class RegisterServicesForApi
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddSingleton<IMinioClient>(_ => new MinioClient()
                .WithEndpoint("localhost")
                .WithCredentials("", "")
                .Build());
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IPhotoService, PhotoService>();
            services.TryAddScoped<ICurrentUser, DummyCurrentUser>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddSingleton<IImageService, ImageService>();
            services.AddSingleton<IS3ResourceService, S3ResourceService>();
            services.AddTransient<IFaceStorageService, FaceStorageService>();
            services.AddPhotoEvents();
            services.AddOptions<TranslatorOptions>().BindConfiguration("Translator");
            services.AddHttpClient<ITranslatorService, TranslatorService>()
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * attempt)));
            services.AddScoped<ISearchFilterNormalizer, SearchFilterNormalizer>();
            services.AddScoped<IEffectiveAccessProvider, EffectiveAccessProvider>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
        }
    }
}
