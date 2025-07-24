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
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IPhotoService, PhotoService>();
            services.AddTransient<ITokenService, TokenService>();
            services.AddTransient<IImageService, ImageService>();
        }
    }
}
