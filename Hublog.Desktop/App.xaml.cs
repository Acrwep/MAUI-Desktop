using Hublog.Desktop.Components.Pages;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

namespace Hublog.Desktop
{
    public partial class App : Application
    {
        private Dashboard _dashboard;

        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
            _dashboard = new Dashboard();

#if WINDOWS
            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
            {
                var nativeWindow = handler.PlatformView;
                var windowHandle = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                TrayIconHelper.InitializeTrayIcon(_dashboard);

                appWindow.Closing += (s, e) =>
                {
                    e.Cancel = true;
                    TrayIconHelper.MinimizeToTray();
                };
            });
#endif
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                window.Width = 390;
                window.Height = 670;
            }

            return window;
        }
    }
}
