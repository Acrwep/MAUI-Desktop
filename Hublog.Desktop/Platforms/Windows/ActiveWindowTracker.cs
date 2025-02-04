using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace Hublog.Desktop.Platforms.Windows
{
    public class ActiveWindowTracker : IActiveWindowTracker
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            Task.Delay(500); // Small delay before retry

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                GetWindowThreadProcessId(handle, out uint processId);
                Process process = Process.GetProcessById((int)processId);
                string applicationName = process.ProcessName.Split(' ')[0];
                Task.Delay(500);

                if (applicationName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
                    applicationName.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
                    applicationName.Equals("firefox", StringComparison.OrdinalIgnoreCase) ||
                    applicationName.Equals("opera", StringComparison.OrdinalIgnoreCase) ||
                    applicationName.Equals("brave", StringComparison.OrdinalIgnoreCase))
                {
                    Task.Delay(500);
                    string browserUrl = GetBrowserUrl(process);
                    if (!string.IsNullOrEmpty(browserUrl))
                    {
                        return $"{applicationName}:{browserUrl}";
                    }
                }

                return applicationName.Trim();
            }

            return string.Empty;
        }


        public string GetBrowserUrl(Process browserProcess)
        {
#if WINDOWS
            return browserProcess.ProcessName switch
            {
                "chrome" or "msedge" => GetChromeEdgeUrl(browserProcess),
                "firefox" => GetFirefoxUrl(browserProcess),
                _ => string.Empty
            };
#else
            return string.Empty;
#endif
        }

#if WINDOWS
        private string GetChromeEdgeUrl(Process process)
        {
            var automationElement = System.Windows.Automation.AutomationElement.FromHandle(process.MainWindowHandle);
            var condition = new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Edit);
            var element = automationElement.FindFirst(System.Windows.Automation.TreeScope.Descendants, condition);
            return element != null ? ((System.Windows.Automation.ValuePattern)element.GetCurrentPattern(System.Windows.Automation.ValuePattern.Pattern)).Current.Value as string : string.Empty;
        }

        private string GetFirefoxUrl(Process process)
        {
            return process.MainWindowTitle;
        }
#endif

        public string GetApplicationIconBase64(string activeAppName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(activeAppName);
                if (processes.Length > 0)
                {
                    string exePath = processes[0].MainModule.FileName;
                    using (Icon icon = Icon.ExtractAssociatedIcon(exePath))
                    {
                        if (icon != null)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                icon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                return Convert.ToBase64String(ms.ToArray()); // Convert to Base64
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting application icon: {ex.Message}");
            }

            return string.Empty; // Return empty if no icon found
        }
    }
}
