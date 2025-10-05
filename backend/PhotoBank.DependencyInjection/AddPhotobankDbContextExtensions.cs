using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
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

            services.AddScoped<PhotoBankDbContext>(sp =>
            {
                var context = sp.GetRequiredService<IDbContextFactory<PhotoBankDbContext>>().CreateDbContext();
                context.ConfigureUser(sp.GetRequiredService<ICurrentUserAccessor>().CurrentUser);
                return context;
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

            services.AddScoped<PhotoBankDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<PhotoBankDbContext>>();
                var context = new PhotoBankDbContext(options);
                context.ConfigureUser(sp.GetRequiredService<ICurrentUserAccessor>().CurrentUser);
                return context;
            });
        }

        services.AddDbContext<AccessControlDbContext>(opt => { opt.UseSqlServer(connectionString); });

        return services;
    }
}
