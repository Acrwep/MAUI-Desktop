using Hublog.Desktop.Components.Pages;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
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

                DisableMaximizeButton(windowHandle);

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
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_THICKFRAME = 0x00040000; 

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOREDRAW = 0x0008;

        private void DisableMaximizeButton(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~WS_MAXIMIZEBOX; 
            style &= ~WS_THICKFRAME;  
            SetWindowLong(hWnd, GWL_STYLE, style);
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, (uint)(SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW));
        }
#endif

    }
}
