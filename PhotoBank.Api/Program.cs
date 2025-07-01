using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using Microsoft.AspNetCore.Identity;
using PhotoBank.Services;
using Serilog.Events;
using Serilog;

namespace PhotoBank.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start App!");

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

            builder.WebHost.UseKestrel();

            builder.WebHost.UseUrls("http://0.0.0.0:5066");

            // Add services to the container.
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException(
                                       "Connection string 'DefaultConnection' not found.");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalFrontend", policy =>
                {
                    policy
                        .WithOrigins("http://192.168.1.45:5173") // IP !
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddDbContext<PhotoBankDbContext>(options =>
            {
                options.UseLoggerFactory(LoggerFactory.Create(loggingBuilder => loggingBuilder.AddDebug()));
                options.UseSqlServer(connectionString,
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

            builder.Services.AddDefaultIdentity<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<PhotoBankDbContext>();

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AllowToSeeAdultContent", policy => {
                    policy.RequireClaim("AllowAdultContent", "True");
                })
                .AddPolicy("AllowToSeeRacyContent", policy => {
                    policy.RequireClaim("AllowRacyContent", "True");
                });
            builder.Services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            RegisterServicesForApi.Configure(builder.Services);
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowLocalFrontend");

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            Console.WriteLine("Run App!");
            app.Run();
        }
    }
}
