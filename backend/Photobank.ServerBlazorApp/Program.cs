using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.ServerBlazorApp.Components.Account;
using Serilog.Events;
using Serilog;
using System.Reflection;
using PhotoBank.ServerBlazorApp.Components;
using PhotoBank.Services;
using Radzen;

namespace PhotoBank.ServerBlazorApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            builder.Services.AddRazorPages();
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
            builder.Services.AddHttpContextAccessor();

            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException(
                                       "Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<PhotoBankDbContext>(options =>
            {
                options.UseLoggerFactory(LoggerFactory.Create(loggingBuilder => loggingBuilder.AddDebug()));
                options.UseNpgsql(connectionString,
                    optionsBuilder =>
                    {
                        optionsBuilder.MigrationsAssembly(typeof(PhotoBankDbContext).GetTypeInfo().Assembly.GetName()
                            .Name);
                        optionsBuilder.UseNetTopologySuite();
                        optionsBuilder.CommandTimeout(120);
                    });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
                    // options are set here
                })
                .AddRoles<IdentityRole>()
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<PhotoBankDbContext>();

            builder.Services.AddAuthorization();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
            RegisterServicesForConsole.Configure(builder.Services, configuration);
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            
            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }
    }
}