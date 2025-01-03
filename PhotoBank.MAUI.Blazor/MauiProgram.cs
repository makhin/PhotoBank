using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Radzen;
using Serilog.Events;
using Serilog;
using System.Reflection;
using PhotoBank.MAUI.Blazor.Services;

namespace PhotoBank.MAUI.Blazor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Load appsettings.json
            using var appsettingsStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("MyApp.wwwroot.appsettings.json");

            if (appsettingsStream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(appsettingsStream)
                    .Build();

                builder.Configuration.AddConfiguration(config);
            }

            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddRadzenComponents();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<IRestService, RestService>();

            return builder.Build();
        }
    }
}
