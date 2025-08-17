using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;

namespace PhotoBank.Services
{
    public static class RegisterServicesForApi
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddSingleton<IImageService, ImageService>();
        }
    }
}
