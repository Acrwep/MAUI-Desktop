using Hublog.Desktop.Entities;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

#if WINDOWS
using System.Windows.Automation;
#endif

namespace Hublog.Desktop
{
    public class ApplicationMonitor
    {
        private readonly HttpClient _httpClient;
        private Dictionary<string, DateTime> appStartTimes = new();
        private Dictionary<string, TimeSpan> appUsageTimes = new();

        public ApplicationMonitor(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task UpdateApplicationOrUrlUsageAsync(string token)
        {
            int userId = GetUserIdFromToken(MauiProgram.token);
            string activeAppOrUrl = GetActiveApplicationName();
            string activeApplicationName = activeAppOrUrl;

            if (!string.IsNullOrEmpty(activeAppOrUrl))
            {
                if (!appStartTimes.ContainsKey(activeAppOrUrl))
                {
                    appStartTimes[activeAppOrUrl] = DateTime.Now;
                }
            }
            else
            {
                if (appStartTimes.Any())
                {
                    foreach (var appOrUrl in appStartTimes.Keys.ToList())
                    {
                        if (appUsageTimes.ContainsKey(appOrUrl))
                        {
                            appUsageTimes[appOrUrl] += DateTime.Now - appStartTimes[appOrUrl];
                        }
                        else
                        {
                            appUsageTimes[appOrUrl] = DateTime.Now - appStartTimes[appOrUrl];
                        }

                        if (IsUrl(appOrUrl))
                        {
                            await SaveUrlUsageDataAsync(userId, appOrUrl);
                            await SaveApplicationUsageDataAsync(userId, appOrUrl);
                        }
                        else
                        {
                            await SaveApplicationUsageDataAsync(userId, appOrUrl);
                        }

                        appStartTimes.Remove(appOrUrl);
                    }
                }
            }
        }



        private string ExtractApplicationName(string appOrUrl)
        {
            var parts = appOrUrl.Split(':');
            return parts.Length > 0 ? parts[0].Trim() : string.Empty;
        }

        private string ExtractUrl(string appOrUrl)
        {
            var parts = appOrUrl.Split(':');
            return parts.Length > 1 ? parts[1].Trim() : string.Empty;
        }

        private bool IsUrl(string appOrUrl)
        {
            return appOrUrl.Contains(".com") || appOrUrl.Contains(".net") || appOrUrl.Contains(".org") || appOrUrl.Contains(".ai") || appOrUrl.Contains(".in") || appOrUrl.Contains("localhost");
        }

        public string GetActiveApplicationName()
        {
            IntPtr handle = GetForegroundWindow();
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                GetWindowThreadProcessId(handle, out uint processId);
                var process = Process.GetProcessById((int)processId);
                string applicationName = process.ProcessName.Split(' ')[0];

                if (applicationName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
                    applicationName.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
                    applicationName.Equals("firefox", StringComparison.OrdinalIgnoreCase))
                {
                    string browserUrl = GetBrowserUrl(process);
                    if (!string.IsNullOrEmpty(browserUrl))
                    {
                        return $"{applicationName}: {browserUrl}";
                    }
                }

                return applicationName.Trim();
            }

            return string.Empty;
        }

        public string GetBrowserUrl(Process browserProcess)
        {
#if WINDOWS
            if (browserProcess.ProcessName == "chrome" || browserProcess.ProcessName == "msedge")
            {
                return GetChromeEdgeUrl(browserProcess);
            }
            else if (browserProcess.ProcessName == "firefox")
            {
                return GetFirefoxUrl(browserProcess);
            }
#endif
            return string.Empty;
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

        private int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        //private async Task SaveApplicationUsageDataAsync(int userId, string applicationName)
        //{
        //    try
        //    {
        //        string appName = ExtractApplicationName(applicationName);

        //        if (appUsageTimes.TryGetValue(applicationName, out TimeSpan usageTime))
        //        {
        //            string totalUsage = $"{(int)usageTime.TotalHours:D2}:{usageTime.Minutes:D2}:{usageTime.Seconds:D2}";

        //            var appUsage = new ApplicationUsage
        //            {
        //                UserId = userId,
        //                ApplicationName = appName,
        //                TotalUsage = totalUsage,
        //                //UsageDate = DateTime.Now.Date,
        //                UsageDate = DateTime.Now.ToString("yyyy-MM-dd"),
        //                Details = $"User spent time on application: {appName}"
        //            };

        //            var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Application", appUsage);
        //            if (!response.IsSuccessStatusCode)
        //            {
        //                Console.WriteLine($"Failed to log application usage: {response.ReasonPhrase}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        private async Task SaveApplicationUsageDataAsync(int userId, string applicationName)
        {
            try
            {
                string appName = ExtractApplicationName(applicationName);

                if (appUsageTimes.TryGetValue(applicationName, out TimeSpan usageTime))
                {
                    string totalUsage = $"{(int)usageTime.TotalHours:D2}:{usageTime.Minutes:D2}:{usageTime.Seconds:D2}";

                    var appUsage = new ApplicationUsage
                    {
                        UserId = userId,
                        ApplicationName = appName,
                        TotalUsage = totalUsage,
                        UsageDate = DateTime.Now.ToString("yyyy-MM-dd"), 
                        Details = $"User spent time on application: {appName}"
                    };

                    var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Application", appUsage);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to log application usage: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving application usage data: {ex.Message}");
            }
        }

        private async Task SaveUrlUsageDataAsync(int userId, string url)
        {
            try
            {
                string actualUrl = ExtractUrl(url);

                if (appUsageTimes.TryGetValue(url, out TimeSpan usageTime))
                {
                    string totalUsage = $"{(int)usageTime.TotalHours:D2}:{usageTime.Minutes:D2}:{usageTime.Seconds:D2}";
                    string baseUrl = new UriBuilder(actualUrl).Uri.Host;  

                    var urlUsage = new UrlUsage
                    {
                        UserId = userId,
                        Url = baseUrl,
                        TotalUsage = totalUsage,
                        UsageDate = DateTime.Now.Date,
                        Details = $"User spent time on URL: {baseUrl}"
                    };

                    var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Url", urlUsage);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to log URL usage: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
