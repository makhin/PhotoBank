using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;

namespace PhotoBank.Services
{
    public static class RegisterServicesForApi
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IPhotoService, PhotoService>();
        }
    }
}
