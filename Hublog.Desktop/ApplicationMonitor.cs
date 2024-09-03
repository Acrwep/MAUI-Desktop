using Hublog.Desktop.Entities;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

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

            string activeApplicationName = GetActiveApplicationName();
            Console.WriteLine($"Active Application Name: {activeApplicationName}");

            if (!string.IsNullOrEmpty(activeApplicationName))
            {
                if (!appStartTimes.ContainsKey(activeApplicationName))
                {
                    appStartTimes[activeApplicationName] = DateTime.Now;
                    Console.WriteLine($"Started tracking application: {activeApplicationName}");
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
                        Console.WriteLine($"Stopped tracking application: {app}. Total usage time: {appUsageTimes[app]}");

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
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                GetWindowThreadProcessId(handle, out uint processId);
                var process = Process.GetProcessById((int)processId);

                return $"{process.ProcessName} - {Buff.ToString()}";
            }
            return string.Empty;
        }

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

        private async Task SaveUsageDataAsync(int userId, string applicationName)
        {
            if (appUsageTimes.TryGetValue(applicationName, out TimeSpan totalUsageTime))
            {
                string usageTime = $"{(int)totalUsageTime.TotalHours:D2}:{totalUsageTime.Minutes:D2}";

                var usage = new ApplicationUsage
                {
                    UserId = userId,
                    ApplicationName = applicationName,
                    TotalUsage = usageTime,
                    Details = $"User spent time on the {applicationName}."
                };

                string jsonPayload = JsonSerializer.Serialize(usage);
                Console.WriteLine($"Sending JSON: {jsonPayload}");

                var response = await _httpClient.PostAsJsonAsync($"{MauiProgram.OnlineURL}api/Users/AppUsage", usage);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to log application usage: {response.ReasonPhrase}");
                }
            }
        }
    }
}
