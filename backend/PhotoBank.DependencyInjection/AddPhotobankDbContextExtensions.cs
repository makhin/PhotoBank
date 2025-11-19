using System;
using System.Threading.Tasks;
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
        // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
        // This is necessary for compatibility when migrating from MS SQL Server
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

                options.UseNpgsql(
                    connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                        npgsql.UseNetTopologySuite();
                        npgsql.UseVector();
                        npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
                        npgsql.CommandTimeout(60);
                        npgsql.MaxBatchSize(128);
                        npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
            });

            services.AddScoped<PhotoBankDbContext>(sp =>
            {
                var context = sp.GetRequiredService<IDbContextFactory<PhotoBankDbContext>>().CreateDbContext();
                context.ConfigureUser(ResolveCurrentUser(sp));
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

                options.UseNpgsql(
                    connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                        npgsql.UseNetTopologySuite();
                        npgsql.UseVector();
                        npgsql.CommandTimeout(120);
                    });
            });

            services.AddScoped<PhotoBankDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<PhotoBankDbContext>>();
                var context = new PhotoBankDbContext(options);
                context.ConfigureUser(ResolveCurrentUser(sp));
                return context;
            });
        }

        services.AddDbContext<AccessControlDbContext>(opt => { opt.UseNpgsql(connectionString); });

        return services;
    }

    private static ICurrentUser ResolveCurrentUser(IServiceProvider sp)
    {
        var accessor = sp.GetService<ICurrentUserAccessor>();
        if (accessor is null)
        {
            return CurrentUser.CreateAnonymous();
        }

        try
        {
            return accessor.CurrentUser;
        }
        catch (InvalidOperationException)
        {
            try
            {
                return accessor.GetCurrentUserAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (InvalidOperationException)
            {
                return CurrentUser.CreateAnonymous();
            }
        }
    }
}



