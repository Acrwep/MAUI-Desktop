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
                    Icon = LoadTrayIcon(),
                    Visible = true,
                    Text = "Hublog"
                };

                _trayIcon.MouseClick += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left || e.Button==MouseButtons.Right)
                    {
                        ShowMainWindow();
                    }
                };

                var contextMenu = new ContextMenuStrip();
                //var quitMenuItem = new ToolStripMenuItem("Quit");
                //quitMenuItem.Click += (s, e) =>
                //{
                //    ConfirmQuit();
                //};

                //contextMenu.Items.Add(quitMenuItem);
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

        private async static void ConfirmQuit()
        {
            var result = MessageBox.Show("Are you sure you want to punch out?", "Confirm Quit", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes && _dashboardInstance != null)
            {
                string closeTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"App closing time: {closeTime}");
                
        // Store the close time using Preferences
        Preferences.Set("closeTime", closeTime);

                _dashboardInstance.PunchOut("user");
                DisposeTrayIcon();
                Microsoft.Maui.Controls.Application.Current.Quit();
            }
        }

        public static void ShowMainWindow()
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

        public static void DisposeTrayIcon()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        public static void QuitApplication()
        {
            DisposeTrayIcon();
            Microsoft.Maui.Controls.Application.Current.Quit();
        }

    }
}
#endif
