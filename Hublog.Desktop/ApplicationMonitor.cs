﻿using Hublog.Desktop.Entities;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using System.Drawing;
using System.Net.WebSockets;
using System.Text.Json;


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
        private bool _isSignalRConnectionStarted = false;  // Flag to track connection state
        private HubConnection _connection;
        private readonly IScreenCaptureService _screenCaptureTracker;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _webSocketCts;
        private bool _isWebSocketConnected = false;

        public ApplicationMonitor(HttpClient httpClient, IScreenCaptureService screenCaptureTracker)
        {
            _httpClient = httpClient;
            _pollingTimer = new Timer(UpdateActiveApplication, null, TimeSpan.Zero, TimeSpan.FromSeconds(1)); // Check every second
            _screenCaptureTracker = screenCaptureTracker;

            // Initialize WebSocket
            _webSocket = new ClientWebSocket();
            _webSocketCts = new CancellationTokenSource();

            EnsureWebSocketConnection();
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
            Console.WriteLine(_webSocket);
            int userId = MauiProgram.Loginlist.Id;
            string activeAppOrUrl = GetActiveApplicationName();
            string extractName = ExtractName(activeAppOrUrl);

            // Listen for the "ReceiveLiveData" event from the SignalR server
            //_connection.On<string, string, string, string, bool, string>("ReceiveLiveData", (userId, organizationId, activeApp, activeUrl, liveStreamStatus, activeAppLogo) =>
            //{
            //    // This will be triggered when the server sends data
            //    Console.WriteLine($"Received data in client: {userId}, {organizationId}, {activeApp}");
            //});

            //if (_isSignalRConnectionStarted == true)
            //{
            //    await SendLiveData();
            //}

            if (_isWebSocketConnected == true)
            {
                await SendLiveDataViaWebSocket();
            }

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
                        await SaveUrlUsageDataAsync(userId, urlName, elapsedTime);
                        await Task.Delay(500);
                        appsAndUrlTimer?.Dispose();
                        timeSpan = TimeSpan.Zero;
                        elapsedTime = "00:00:00";
                        appsAndUrlTimer = new System.Threading.Timer(UpdateTimer, null, 1000, 1000);
                    }
                    else
                    {
                        await SaveApplicationUsageDataAsync(userId, appName, elapsedTime);
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
            if (appOrUrl == null || appOrUrl == "")
            {
                return "";
            }
            var parts = appOrUrl.Split(':');
            return parts.Length > 0 ? parts[0].Trim() : string.Empty;
        }

        private string ExtractUrl(string appOrUrl)
        {
            if (string.IsNullOrEmpty(appOrUrl) || appOrUrl == "msedge" || appOrUrl == "chrome" || appOrUrl == "firefox" || appOrUrl == "opera" || appOrUrl == "brave")
            {
                return "";
            }

            // Split the string into two parts based on the colon ":"
            var parts = appOrUrl.Split(new[] { ':' }, 2); // Split into two parts only
            if (parts.Length > 1)
            {
                string extractedUrl = parts[1].Trim(); // Extract the URL part after ":"

                if (extractedUrl.Contains(" — "))
                {
                    //firebox url handling
                    string[] split = extractedUrl.Split(new[] { " — " }, StringSplitOptions.None);
                    return $"{split[0].Trim().ToLower()}.com";
                }
                else if (extractedUrl.Contains(" - "))
                {
                    //opera url handling
                    string[] split = extractedUrl.Split(new[] { " - " }, StringSplitOptions.None);
                    return $"{split[0].Trim().ToLower()}.com";
                }
                else if (!extractedUrl.StartsWith("https://") && !extractedUrl.StartsWith("http://"))
                {
                    //chrome url handling
                    string domain = extractedUrl.Split('/')[0];
                    return domain;
                }
                else
                {
                    //microsioftedge url handling
                    try
                    {
                        Uri uri = new Uri(extractedUrl);
                        return uri.Host; // Extract the domain (e.g., chatgpt.com)
                    }
                    catch
                    {
                        return extractedUrl; // Return as is if parsing fails
                    }
                }
            }
            return string.Empty;
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
                    applicationName.Equals("Mozilla Firefox", StringComparison.OrdinalIgnoreCase) ||
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
                "firefox" or "opera" or "brave" => GetExternalBrowserUrl(browserProcess),
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

        private string GetExternalBrowserUrl(Process process)
        {
            return process.MainWindowTitle;
        }
#endif

        private bool FinalApplicationNameValidation(string applicationName)
        {
            // Remove all non-alphabetic characters and count only alphabetic letters
            var letterCount = applicationName.Count(char.IsLetter);

            // Check if there are at least two alphabetic letters
            return letterCount >= 2;
        }

        private async Task SaveApplicationUsageDataAsync(int userId, string applicationName, string totalUsage)
        {
            if (string.IsNullOrEmpty(applicationName) || applicationName == "msedge" || applicationName == "chrome" || applicationName == "firefox" || applicationName == "opera" || applicationName == "brave" || applicationName == null || applicationName == "null" || applicationName == "")
            {
                return;
            }
            bool finalValidationStatus = FinalApplicationNameValidation(applicationName);

            if (finalValidationStatus == true)
            {
                try
                {
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
            else
            {
                Console.WriteLine("Application name not valid");
            }
        }

        private static bool FinalUrlValidation(string appOrUrl)
        {
            return appOrUrl.Contains(".com") || appOrUrl.Contains(".net") || appOrUrl.Contains(".org") || appOrUrl.Contains(".ai") || appOrUrl.Contains(".in") || appOrUrl.Contains("localhost");
        }

        private async Task SaveUrlUsageDataAsync(int userId, string url, string totalUsage)
        {
            bool finalValidationStatus = FinalUrlValidation(url);

            if (finalValidationStatus == true)
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
            else
            {
                Console.WriteLine("Url not valid");
            }
        }

        //handle after punchout
        public async Task LastApplicationOrUrlUsageAsync()
        {
            int userId = MauiProgram.Loginlist.Id;
            Console.WriteLine(_previousAppOrUrl);

            if (_previousFullAppOrUrl == "" || _previousFullAppOrUrl == null)
            {
                return;
            }
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
            if (string.IsNullOrEmpty(applicationName) || applicationName == "msedge" || applicationName == "chrome" || applicationName == "firefox" || applicationName == "opera" || applicationName == "brave" || applicationName == null || applicationName == "null" || applicationName == "")
            {
                //await StopSignalR();
                await StopWebSocket();
                return;
            }
            bool finalValidationStatus = FinalApplicationNameValidation(applicationName);

            if (finalValidationStatus == true)
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
                    //await StopSignalR();
                    await StopWebSocket();
                }
            }
            else
            {
                Console.WriteLine("Application name not valid");
            }
        }

        private async Task LastSaveUrlUsageDataAsync(int userId, string url, string totalUsage)
        {
            //await StopSignalR();
            await StopWebSocket();
            bool finalValidationStatus = FinalUrlValidation(url);

            if (finalValidationStatus == true)
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
                    //await StopSignalR();
                    await StopWebSocket();
                }
            }
            else
            {
                Console.WriteLine("Url not valid");
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        //livestream code

        private async Task EnsureWebSocketConnection()
        {
            if (!_isWebSocketConnected)
            {
                await StartWebSocket();
            }
        }

        public async Task StartWebSocket()
        {
            if (_isWebSocketConnected || _webSocket?.State == WebSocketState.Open)
            {
                Console.WriteLine("WebSocket is already connected.");
                return;
            }

            try
            {
                // Recreate if disposed
                if (_webSocket == null)
                {
                    _webSocket = new ClientWebSocket();
                    _webSocketCts = new CancellationTokenSource();
                }

                // Bypass SSL for development (remove in production)
#if DEBUG
                _webSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
#endif

                var wsUri = new Uri("wss://localhost:7263/ws/livestream");
                await _webSocket.ConnectAsync(wsUri, _webSocketCts.Token);
                _isWebSocketConnected = true;
                Console.WriteLine("Connected to WebSocket");

                // Start listening for messages
                _ = ReceiveWebSocketMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
                _isWebSocketConnected = false;
                await ReconnectWebSocket();
            }
        }

        private async Task ReconnectWebSocket()
        {
            try
            {
                await Task.Delay(5000); // Wait 5 seconds before reconnecting
                await StartWebSocket();
            }
            catch
            {
                // Ignore reconnection errors
            }
        }

        private async Task ReceiveWebSocketMessages()
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket?.State == WebSocketState.Open &&
                      !_webSocketCts.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _webSocketCts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            CancellationToken.None);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    // Process server messages here
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket receive error: {ex.Message}");
                _isWebSocketConnected = false;
                await ReconnectWebSocket();
            }
        }

        private async Task SendLiveDataViaWebSocket()
        {
            try
            {
                string activeAppandUrl = GetActiveApplicationName();
                string activeApp = ExtractApplicationName(activeAppandUrl);
                bool validateActiveApp = FinalApplicationNameValidation(activeApp);
                string activeUrl = ExtractUrl(activeAppandUrl);
                bool validateActiveUrl = FinalUrlValidation(activeUrl);
                string activeApplogo = validateActiveApp ? GetApplicationIconBase64(activeApp) : "";
                byte[] screenshotData;
                string screenshotAsBase64 = string.Empty;

                // Get Location Data
                var location = await GetCurrentLocation();
                double latitude = 0.0, longitude = 0.0;
                if (location is not null)
                {
                    latitude = (double)location.GetType().GetProperty("Latitude")?.GetValue(location, null);
                    longitude = (double)location.GetType().GetProperty("Longitude")?.GetValue(location, null);
                }

                try
                {
                    screenshotData = _screenCaptureTracker.CaptureScreen();
                    screenshotAsBase64 = await UploadScreenshot(screenshotData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Screenshot Error: {ex.Message}");
                    screenshotAsBase64 = "";
                }
                var payload = new
                {
                    userId = MauiProgram.Loginlist.Id,
                    organizationId = MauiProgram.Loginlist.OrganizationId,
                    activeApp = validateActiveApp ? activeApp : "",
                    activeUrl = validateActiveUrl ? activeUrl.Contains("localhost") ? "localhost" : activeUrl : "",
                    liveStreamStatus = true,
                    activeAppLogo = activeApplogo,
                    activeScreenshot = screenshotAsBase64,
                    latitude = latitude,
                    longitude = longitude
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var bytes = Encoding.UTF8.GetBytes(jsonPayload);

                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    if (bytes != null && _webSocketCts != null)
                    {
                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            true,
                            _webSocketCts.Token);
                    }
                    else
                    {
                        Console.WriteLine("Bytes or CancellationTokenSource is null.");
                    }
                }
                else
                {
                    Console.WriteLine("WebSocket is not connected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending WebSocket data: {ex.Message}");
                _isWebSocketConnected = false;
            }
        }

        public async Task StopWebSocket()
        {
            //send last status befor stopping
            var payload = new
            {
                userId = MauiProgram.Loginlist.Id,
                organizationId = MauiProgram.Loginlist.OrganizationId,
                activeApp = "",
                activeUrl = "",
                liveStreamStatus = false,
                activeAppLogo = "",
                activeScreenshot = "",
                latitude = 0.0,
                longitude = 0.0
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(jsonPayload);

            try
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _webSocketCts.Token);

                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client closing",
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending or closing WebSocket: {ex.Message}");
            }
            finally
            {
                _isWebSocketConnected = false;
                _webSocketCts?.Cancel();
                _webSocket?.Dispose();
                _webSocket = null;
            }
        }

        private async Task CaptureAndUploadScreenshot()
        {
            try
            {
                var screenshotData = _screenCaptureTracker.CaptureScreen();

                await UploadScreenshot(screenshotData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screenshot Error: {ex.Message}");
            }
        }
        private async Task<string> UploadScreenshot(byte[] imageData)
        {
            string base64Image = Convert.ToBase64String(imageData);
            Console.WriteLine("Base64 Image Data: " + base64Image); // Log base64 image data
            return base64Image;
        }

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

        public async Task<object> GetCurrentLocation()
        {
            try
            {
                var location = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                    return await Geolocation.GetLocationAsync(request);
                });

                if (location != null)
                {
                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}");

                    return new
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    };
                }
                else
                {
                    Console.WriteLine("Unable to get location.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return new { Latitude = 0.0, Longitude = 0.0 }; // Default if location not found
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
