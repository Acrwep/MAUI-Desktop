﻿using Hublog.Desktop.Entities;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Hublog.Desktop
{
    public static class MauiProgram
    {
        public static string OnlineURL = "https://localhost:7263/";
        //public static string OnlineURL = "https://hublog.org:8086/";

        public static Users Loginlist = new Users();
        public static string token = "";
        public static Mutex mutex = new Mutex(true, "HublogAppUniqueMutex");

        public static MauiApp CreateMauiApp()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                // Application is already running, exit the program
#if WINDOWS
                Microsoft.UI.Xaml.Application.Current.Exit();
#endif
                Environment.Exit(0);
            }

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
