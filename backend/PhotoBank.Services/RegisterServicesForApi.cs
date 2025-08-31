using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.AddMemoryCache();
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
        }
    }
}
