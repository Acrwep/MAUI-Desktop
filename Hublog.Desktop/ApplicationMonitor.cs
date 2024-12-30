using Hublog.Desktop.Entities;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

#if WINDOWS
using System.Windows.Automation;
#endif

namespace Hublog.Desktop
{
    public class ApplicationMonitor
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, DateTime> _appStartTimes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TimeSpan> _appUsageTimes = new();
        private readonly Timer _pollingTimer;
        private bool triggerStatus = false;
        private string _previousAppOrUrl;
        private string _previousFullAppOrUrl;
        private string elapsedTime = "00:00:00";
        private TimeSpan timeSpan = TimeSpan.Zero;
        private System.Threading.Timer appsAndUrlTimer;

        public ApplicationMonitor(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _pollingTimer = new Timer(UpdateActiveApplication, null, TimeSpan.Zero, TimeSpan.FromSeconds(1)); // Check every second
        }

        private void UpdateActiveApplication(object state)
        {
            if (triggerStatus == false)
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000); 
                _ = UpdateApplicationOrUrlUsageAsync(""); // Call the method asynchronously
                triggerStatus = true;
            }
        }

        private void UpdateTimer(object state)
        {
            timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
            elapsedTime = timeSpan.ToString(@"hh\:mm\:ss");
        }

        public async Task UpdateApplicationOrUrlUsageAsync(string token)
        {
            int userId = MauiProgram.Loginlist.Id;
            string activeAppOrUrl = GetActiveApplicationName();
            string extractName = ExtractName(activeAppOrUrl);

            if (!string.IsNullOrEmpty(activeAppOrUrl))
            {
                if (extractName != _previousAppOrUrl)
                {
                    if (_previousAppOrUrl == null || _previousAppOrUrl == "")
                    {
                        _previousAppOrUrl = extractName;
                        _previousFullAppOrUrl = activeAppOrUrl;
                        return;
                    }
                    string appName = ExtractApplicationName(_previousFullAppOrUrl);
                    string urlName = ExtractUrl(_previousFullAppOrUrl);

                    if (!string.IsNullOrEmpty(urlName))
                    {
                        await SaveUrlUsageDataAsync(userId, _previousAppOrUrl, elapsedTime);
                        await Task.Delay(500);
                        appsAndUrlTimer?.Dispose();
                        timeSpan = TimeSpan.Zero;
                        elapsedTime = "00:00:00";
                        appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);
                    }
                    else
                    {
                        await SaveApplicationUsageDataAsync(userId, _previousAppOrUrl, elapsedTime);
                        await Task.Delay(500);
                        appsAndUrlTimer?.Dispose();
                        timeSpan = TimeSpan.Zero;
                        elapsedTime = "00:00:00";
                        appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);
                    }
                    _previousAppOrUrl = extractName;
                    _previousFullAppOrUrl = activeAppOrUrl;
                }
                else
                {
                    Console.WriteLine(elapsedTime);
                }
            }
        }

        public static string ExtractName(string url)
        {
            var match = Regex.Match(url, @":([^/]+)");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return url;  // Return empty if no match is found
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

        private async Task SaveApplicationUsageDataAsync(int userId, string applicationName, string totalUsage)
        {
            try
            {

                //string totalUsage = FormatTimeSpan(usageTime);
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);

                var appUsage = new ApplicationUsage
                {
                    UserId = userId,
                    ApplicationName = applicationName,
                    TotalUsage = totalUsage,
                    UsageDate = DateTime.Now.Date,
                    Details = $"User spent time on application: {applicationName}"
                };
                var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Application", appUsage);
                _appStartTimes.Remove(applicationName);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to log application usage: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving application usage: {ex.Message}");
            }
            finally
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);
            }
        }

        private async Task SaveUrlUsageDataAsync(int userId, string url, string totalUsage)
        {
            try
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);


                var urlUsage = new UrlUsage
                {
                    UserId = userId,
                    Url = url.Contains("localhost") ? "localhost" : url,
                    TotalUsage = totalUsage,
                    UsageDate = DateTime.Now.Date,
                    Details = $"User spent time on URL: {url}"
                };

                var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Url", urlUsage);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to log URL usage: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving URL usage: {ex.Message}");
            }
            finally
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);
            }
        }

        //handle after punchout
        public async Task LastApplicationOrUrlUsageAsync()
        {
            int userId = MauiProgram.Loginlist.Id;
            Console.WriteLine(_previousAppOrUrl);

            string appName = ExtractApplicationName(_previousFullAppOrUrl);
            string urlName = ExtractUrl(_previousFullAppOrUrl);
            if (!string.IsNullOrEmpty(urlName))
            {
                await LastSaveUrlUsageDataAsync(userId, _previousAppOrUrl, elapsedTime);
                await Task.Delay(500);
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
            }
            else
            {
                await LastSaveApplicationUsageDataAsync(userId, _previousAppOrUrl, elapsedTime);
                await Task.Delay(500);
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
            }
        }

        private async Task LastSaveApplicationUsageDataAsync(int userId, string applicationName, string totalUsage)
        {
            try
            {

                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                _previousAppOrUrl = string.Empty;
                _previousFullAppOrUrl = string.Empty;

                var appUsage = new ApplicationUsage
                {
                    UserId = userId,
                    ApplicationName = applicationName,
                    TotalUsage = totalUsage,
                    UsageDate = DateTime.Now.Date,
                    Details = $"User spent time on application: {applicationName}"
                };
                var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Application", appUsage);
                _appStartTimes.Remove(applicationName);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to log application usage: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving application usage: {ex.Message}");
            }
            finally
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                _previousAppOrUrl = string.Empty;
                _previousFullAppOrUrl = string.Empty;
                triggerStatus = true;
            }
        }

        private async Task LastSaveUrlUsageDataAsync(int userId, string url, string totalUsage)
        {
            try
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                _previousAppOrUrl = string.Empty;
                _previousFullAppOrUrl = string.Empty;

                var urlUsage = new UrlUsage
                {
                    UserId = userId,
                    Url = url.Contains("localhost") ? "localhost" : url,
                    TotalUsage = totalUsage,
                    UsageDate = DateTime.Now.Date,
                    Details = $"User spent time on URL: {url}"
                };

                var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/Url", urlUsage);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to log URL usage: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving URL usage: {ex.Message}");
            }
            finally
            {
                appsAndUrlTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                elapsedTime = "00:00:00";
                _previousAppOrUrl = string.Empty;
                _previousFullAppOrUrl = string.Empty;
                triggerStatus = true;
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
