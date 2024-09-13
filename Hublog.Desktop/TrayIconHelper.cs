#if WINDOWS
using Hublog.Desktop.Components.Pages;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinRT.Interop;

namespace Hublog.Desktop
{
    public static class TrayIconHelper
    {
        private static NotifyIcon _trayIcon;
        private static Dashboard _dashboardInstance;

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int SW_SHOWMINIMIZED = 2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public static void InitializeTrayIcon(Dashboard dashboard)
        {
            _dashboardInstance = dashboard;

            if (_trayIcon == null)
            {
                _trayIcon = new NotifyIcon
                {
                    //Icon = SystemIcons.Application,
                    Icon = LoadTrayIcon(),
                    Visible = true,
                    Text = "Hublog"
                };

                _trayIcon.DoubleClick += (sender, e) =>
                {
                    ShowMainWindow();
                };

                var contextMenu = new ContextMenuStrip();
                var quitMenuItem = new ToolStripMenuItem("Quit");
                quitMenuItem.Click += (s, e) =>
                {
                    ConfirmQuit();
                };

                contextMenu.Items.Add(quitMenuItem);
                _trayIcon.ContextMenuStrip = contextMenu;
            }
        }

        private static Icon LoadTrayIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Images", "hublog.ico");

            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
            else
            {
                return SystemIcons.Application;
            }
        }



        private static void ConfirmQuit()
        {
            var result = MessageBox.Show("Are you sure you want to punch out?", "Confirm Quit", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes && _dashboardInstance != null)
            {
                _dashboardInstance.PunchOut();
                DisposeTrayIcon();
                Microsoft.Maui.Controls.Application.Current.Quit(); 
            }
        }

        private static void ShowMainWindow()
        {
            var mainWindow = App.Current.Windows[0];
            var nativeWindow = mainWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window;

            if (nativeWindow != null)
            {
                var handle = WindowNative.GetWindowHandle(nativeWindow);

                ShowWindow(handle, SW_RESTORE);
                SetForegroundWindow(handle);

                int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
                SetWindowLong(handle, GWL_EXSTYLE, exStyle & ~WS_EX_TOOLWINDOW);
            }
        }

        public static void MinimizeToTray()
        {
            var mainWindow = App.Current.Windows[0];
            var nativeWindow = mainWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window;

            if (nativeWindow != null)
            {
                var handle = WindowNative.GetWindowHandle(nativeWindow);

                ShowWindow(handle, SW_SHOWMINIMIZED);

                int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
                SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
                _trayIcon.Visible = true;
            }
        }

        public static void RestoreToTaskbar()
        {
            var mainWindow = App.Current.Windows[0];
            var nativeWindow = mainWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window;

            if (nativeWindow != null)
            {
                var handle = WindowNative.GetWindowHandle(nativeWindow);

                ShowWindow(handle, SW_SHOW);

                int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
                SetWindowLong(handle, GWL_EXSTYLE, exStyle & ~WS_EX_TOOLWINDOW);

                SetForegroundWindow(handle);
            }
        }

        public static void DisposeTrayIcon()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }
}
#endif