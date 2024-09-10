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

        public async Task UpdateApplicationUsageAsync(string token)
        {
            int userId = GetUserIdFromToken(MauiProgram.token);
            Console.WriteLine($"User ID from token: {userId}");

            string activeApplicationNameOrUrl = GetActiveApplicationName();
            Console.WriteLine($"Active Application/URL: {activeApplicationNameOrUrl}");

            if (!string.IsNullOrEmpty(activeApplicationNameOrUrl))
            {
                if (!appStartTimes.ContainsKey(activeApplicationNameOrUrl))
                {
                    appStartTimes[activeApplicationNameOrUrl] = DateTime.Now;
                    Console.WriteLine($"Started tracking application/URL: {activeApplicationNameOrUrl}");
                }
            }
            else
            {
                if (appStartTimes.Any())
                {
                    foreach (var app in appStartTimes.Keys.ToList())
                    {
                        if (appUsageTimes.ContainsKey(app))
                        {
                            appUsageTimes[app] += DateTime.Now - appStartTimes[app];
                        }
                        else
                        {
                            appUsageTimes[app] = DateTime.Now - appStartTimes[app];
                        }

                        await SaveUsageDataAsync(userId, app);
                        Console.WriteLine($"Stopped tracking application/URL: {app}. Total usage time: {appUsageTimes[app]}");

                        appStartTimes.Remove(app);
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
                    else
                    {
                        return applicationName;
                    }
                }

                return applicationName.Trim();
            }

            return string.Empty;
        }


        public string GetBrowserUrl(Process browserProcess)
        {
            if (browserProcess.ProcessName == "chrome" || browserProcess.ProcessName == "msedge")
            {
#if WINDOWS
                return GetChromeEdgeUrl(browserProcess);
#endif
            }
            else if (browserProcess.ProcessName == "firefox")
            {
#if WINDOWS
                return GetFirefoxUrl(browserProcess);
#endif
            }

            return string.Empty;
        }

#if WINDOWS
        private string GetChromeEdgeUrl(Process process)
        {
            try
            {
                var automationElement = System.Windows.Automation.AutomationElement.FromHandle(process.MainWindowHandle);
                var condition = new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Edit);
                var element = automationElement.FindFirst(System.Windows.Automation.TreeScope.Descendants, condition);
                if (element != null)
                {
                    return ((System.Windows.Automation.ValuePattern)element.GetCurrentPattern(System.Windows.Automation.ValuePattern.Pattern)).Current.Value as string;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting browser URL: {ex.Message}");
            }
            return string.Empty;
        }
#endif

#if WINDOWS
        private string GetFirefoxUrl(Process process)
        {
            try
            {
                var title = process.MainWindowTitle;
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Firefox URL: {ex.Message}");
            }
            return string.Empty;
        }
#endif

        private int GetUserIdFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Token is null or empty. Cannot extract user ID.");
                return 0;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

                if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value))
                {
                    return int.Parse(userIdClaim.Value);
                }
                else
                {
                    Console.WriteLine("User ID claim not found or has an invalid value.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JWT token: {ex.Message}");
                return 0;
            }
        }

        private async Task SaveUsageDataAsync(int userId, string applicationOrUrl)
        {
            bool isUrl = applicationOrUrl.Contains(".com") || applicationOrUrl.Contains(".ai") || applicationOrUrl.Contains(".in");

            string baseUrl = isUrl ? ExtractBaseUrl(applicationOrUrl) : null;

            string applicationName = isUrl ? GetBrowserNameFromUrl(applicationOrUrl) : applicationOrUrl;
            string url = isUrl ? baseUrl : null;
            string details = isUrl ? $"User spent time on the URL: {baseUrl}" : $"User spent time on application: {applicationOrUrl}";

            if (applicationOrUrl.Contains(":"))
            {
                var parts = applicationOrUrl.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    applicationName = parts[0].Trim();
                    url = isUrl ? ExtractBaseUrl(parts[1].Trim()) : null;
                }
            }

            if (appUsageTimes.TryGetValue(applicationOrUrl, out TimeSpan totalUsageTime))
            {
                string usageTime = $"{(int)totalUsageTime.TotalHours:D2}:{totalUsageTime.Minutes:D2}:{totalUsageTime.Seconds:D2}";

                var usage = new ApplicationUsage
                {
                    UserId = userId,
                    ApplicationName = applicationName,
                    TotalUsage = usageTime,
                    Details = details,
                    UsageDate = DateTime.Now.Date,
                    Url = url
                };

                string jsonPayload = JsonSerializer.Serialize(usage);
                Console.WriteLine($"Sending JSON: {jsonPayload}");

                var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/AppsUrls/AppUsage", usage);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to log application/URL usage: {response.ReasonPhrase}");
                }
            }
        }



        private string ExtractBaseUrl(string url)
        {
            string urlToParse = url;
            if (urlToParse.Contains(":"))
            {
                var parts = urlToParse.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    urlToParse = parts[1].Trim();
                }
            }

            try
            {
                var uri = new UriBuilder(urlToParse).Uri; 
                string host = uri.Host;

                return urlToParse.Substring(0, urlToParse.IndexOf(host) + host.Length);
            }
            catch (UriFormatException)
            {
                Console.WriteLine($"Invalid URL format: {url}");
                return urlToParse; 
            }
        }



        private string GetBrowserNameFromUrl(string url)
        {
            if (url.Contains("chrome", StringComparison.OrdinalIgnoreCase))
            {
                return "Chrome";
            }
            else if (url.Contains("edge", StringComparison.OrdinalIgnoreCase))
            {
                return "Edge";
            }
            else if (url.Contains("firefox", StringComparison.OrdinalIgnoreCase))
            {
                return "Firefox";
            }

            return "Unknown Browser";
        }
    }
}
