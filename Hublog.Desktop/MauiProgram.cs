using Hublog.Desktop.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform;
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
            //Cleaning the cache .uncommand and run once before taking build.
            //string userDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hublog");
            //if (Directory.Exists(userDataPath))
            //{
            //    Directory.Delete(userDataPath, true); // Delete all data in the folder
            //}

            // Set the environment variable for the WebView2 user data folder
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hublog"));
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
#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        window.Title = "Hublog";
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow =
                        Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                        var titleBar = appWindow.TitleBar;
                        titleBar.ExtendsContentIntoTitleBar = false; //hide default title bar
                        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                        {
                            presenter.IsResizable = false;
                            presenter.SetBorderAndTitleBar(false, false);
                            presenter.IsMaximizable = false; // Prevent maximizing
                        }
                    });
                });
            });
#endif
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddHttpClient();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
#if WINDOWS
            builder.Services.AddSingleton<IScreenCaptureService, Platforms.Windows.WindowsScreenCaptureService>();
            builder.Services.AddSingleton<LiveStreamClient>(); // Register LiveStreamClient as singleton
            builder.Services.AddSingleton<IActiveWindowTracker, Platforms.Windows.ActiveWindowTracker>();
#endif
            return builder.Build();

        }
    }
}
