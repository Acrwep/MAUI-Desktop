#if WINDOWS
using Hublog.Desktop.Components.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Hublog.Desktop
{
    public partial class App : Application
    {
        private Dashboard _dashboard;

        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();

#if WINDOWS
            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
            {
                var nativeWindow = handler.PlatformView;
                var windowHandle = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                TrayIconHelper.InitializeTrayIcon(_dashboard);

                HideMinimizeAndMaximizeButtons(windowHandle); 

                appWindow.Closing += (s, e) =>
                {
                    if (MauiProgram.Loginlist == null || !MauiProgram.Loginlist.Active)
                    {
                        TrayIconHelper.QuitApplication();
                    }
                    else
                    {
                        e.Cancel = true;
                        TrayIconHelper.MinimizeToTray();
                    }
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

#if WINDOWS
        private const int GWL_STYLE = -16;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_THICKFRAME = 0x40000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void HideMinimizeAndMaximizeButtons(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~WS_MAXIMIZEBOX; 
            style &= ~WS_MINIMIZEBOX; 
            SetWindowLong(hWnd, GWL_STYLE, style);
        }
#endif

    }
}
#endif