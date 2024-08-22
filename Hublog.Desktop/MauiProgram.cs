using Hublog.Desktop.Entities;
using Microsoft.Extensions.Logging;

namespace Hublog.Desktop
{
    public static class MauiProgram
    {
        public static string OnlineURL = "https://localhost:44322/";
        //public static string OnlineURL = "https://localhost:7263/";

        //public static TokenClaims UserClaims { get; set; }
        public static Users Loginlist = new Users();
        public static string token = "";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddHttpClient();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif
#if WINDOWS
            builder.Services.AddSingleton<IScreenCaptureService, Platforms.Windows.WindowsScreenCaptureService>();
#endif
            return builder.Build();

        }
    }
}
