using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Radzen;
using System.Reflection;

namespace PhotoBank.MAUI.Blazor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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

            return builder.Build();
        }
    }
}
