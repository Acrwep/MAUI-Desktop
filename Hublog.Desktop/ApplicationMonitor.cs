using Hublog.Desktop.Entities;
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
        private CancellationTokenSource _metadataCts; // 👈 Add this line
        private bool _appLogoSent = false;
        //private string _currentMode = "metadata"; // default to sending metadata
        private int _screenshotRequestUserId = 0; // Track who requested screenshot

        // Add these at class level
        private enum ConnectionMode { Metadata, Screenshot }
        private ConnectionMode _currentMode = ConnectionMode.Metadata;
        private readonly SemaphoreSlim _modeLock = new SemaphoreSlim(1, 1);
        private DateTime _lastScreenshotRequest = DateTime.MinValue;

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

        public async Task EnsureWebSocketConnection()
        {
            if (!_isWebSocketConnected)
            {
                await StartWebSocket();
            }
        }

        // Start the WebSocket connection
        public async Task StartWebSocket()
        {
            if (_isWebSocketConnected || (_webSocket != null && _webSocket.State == WebSocketState.Open))
            {
                Console.WriteLine("WebSocket is already connected.");
                return;
            }

            try
            {
                _webSocketCts = new CancellationTokenSource();
                _metadataCts = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

#if DEBUG
                _webSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
#endif

                // 🔄 Replace with your SERVER IP or actual domain
                var wsUri = new Uri("wss://192.168.1.25:7263/ws/livestream");
                await _webSocket.ConnectAsync(wsUri, _webSocketCts.Token);

                _isWebSocketConnected = true;
                _appLogoSent = false;

                Console.WriteLine("✅ Connected to WebSocket");

                _ = ReceiveWebSocketMessages();
                _ = StartSendingMetadataLoop();
                _ = SendHeartbeatLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebSocket connection error: {ex.Message}");
                _isWebSocketConnected = false;
                await ReconnectWebSocket();
            }
        }
        // Reconnect with delay
        private async Task ReconnectWebSocket()
        {
            try
            {
                Console.WriteLine("🔁 Reconnecting WebSocket in 5 seconds...");
                await Task.Delay(5000);
                await StartWebSocket();
            }
            catch { }
        }

        // Receive and handle incoming WebSocket messages
        private async Task ReceiveWebSocketMessages()
        {
            var buffer = new byte[8192];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !_webSocketCts.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _webSocketCts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("🔌 WebSocket closed by server.");
                        _isWebSocketConnected = false;

                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested", CancellationToken.None);
                        await ReconnectWebSocket();
                        return;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"📩 Received: {message}");

                    await HandleWebSocketMessage(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ WebSocket receive error: {ex.Message}");
                _isWebSocketConnected = false;
                await ReconnectWebSocket();
            }
        }
        // Handle received commands
        private async Task HandleWebSocketMessage(string message)
        {
            try
            {
                Console.WriteLine($"🔍 Parsing message: {message}");

                using JsonDocument doc = JsonDocument.Parse(message);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("type", out JsonElement typeElement))
                {
                    string type = typeElement.GetString();
                    Console.WriteLine($"📦 Message type: {type}");

                    await _modeLock.WaitAsync();
                    try
                    {
                        if (type == "requestScreenshot")
                        {
                            Console.WriteLine("📸 Screenshot request received.");

                            if (root.TryGetProperty("userId", out JsonElement userIdElement))
                            {
                                int userId = userIdElement.GetInt32();

                                Console.WriteLine($"🆔 Message userId: {userId}, Local userId: {MauiProgram.Loginlist.Id}");

                                if (userId == MauiProgram.Loginlist.Id)
                                {
                                    _currentMode = ConnectionMode.Screenshot;
                                    _lastScreenshotRequest = DateTime.UtcNow;
                                    await SendScreenshot();
                                }
                                else
                                {
                                    Console.WriteLine("🚫 Screenshot request not for this user.");
                                }
                            }
                        }
                        else if (type == "resumeMetadata")
                        {
                            _currentMode = ConnectionMode.Metadata;
                            Console.WriteLine("🔄 Resume metadata requested.");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Unhandled message type: {type}");
                        }
                    }
                    finally
                    {
                        _modeLock.Release();
                    }
                }
                else
                {
                    Console.WriteLine("❗ No 'type' found in message.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error handling message: {ex.Message}");
            }
        }
        // Send metadata every second
        private async Task StartSendingMetadataLoop()
        {
            try
            {
                while (!_metadataCts.IsCancellationRequested)
                {
                    await _modeLock.WaitAsync();
                    try
                    {
                        if (_currentMode == ConnectionMode.Metadata)
                        {
                            await SendLiveDataViaWebSocket();
                        }
                        // Auto-resume metadata if screenshot was taken more than 5 seconds ago
                        else if (_currentMode == ConnectionMode.Screenshot &&
                                (DateTime.UtcNow - _lastScreenshotRequest).TotalSeconds > 5)
                        {
                            _currentMode = ConnectionMode.Metadata;
                            Console.WriteLine("Automatically resuming metadata after screenshot");
                        }
                    }
                    finally
                    {
                        _modeLock.Release();
                    }

                    await Task.Delay(1000, _metadataCts.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Metadata loop error: {ex.Message}");
            }
        }
        // Send a ping message every 5 seconds to keep connection alive
        private async Task SendHeartbeatLoop()
        {
            try
            {
                while (_webSocket != null && _webSocket.State == WebSocketState.Open && !_webSocketCts.IsCancellationRequested)
                {
                    var pingMessage = Encoding.UTF8.GetBytes("{\"type\":\"ping\"}");
                    await _webSocket.SendAsync(new ArraySegment<byte>(pingMessage),
                        WebSocketMessageType.Text,
                        true,
                        _webSocketCts.Token);

                    // Increase delay to 30 seconds (standard WebSocket ping interval)
                    await Task.Delay(30000, _webSocketCts.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Heartbeat error: {ex.Message}");
            }
        }

        // Send metadata and application icon if not sent already
        private async Task SendLiveDataViaWebSocket()
        {
            //try
            //{
            //    string activeAppAndUrl = GetActiveApplicationName();
            //    string activeApp = ExtractApplicationName(activeAppAndUrl);
            //    bool isValidApp = FinalApplicationNameValidation(activeApp);
            //    string activeUrl = ExtractUrl(activeAppAndUrl);
            //    bool isValidUrl = FinalUrlValidation(activeUrl);

            //    byte[] appLogoBytes = isValidApp ? GetApplicationIconBytes(activeApp) : Array.Empty<byte>();

            //    var location = await GetCurrentLocation();
            //    double latitude = 0.0, longitude = 0.0;
            //    if (location != null)
            //    {
            //        latitude = (double)(location.GetType().GetProperty("Latitude")?.GetValue(location, null) ?? 0.0);
            //        longitude = (double)(location.GetType().GetProperty("Longitude")?.GetValue(location, null) ?? 0.0);
            //    }
            //    var metadata = new
            //    {
            //        type = "metadata",
            //        userId = MauiProgram.Loginlist.Id,
            //        organizationId = MauiProgram.Loginlist.OrganizationId,
            //        activeApp = isValidApp ? activeApp : "",
            //        activeUrl = isValidUrl ? (activeUrl.Contains("localhost") ? "localhost" : activeUrl) : "",
            //        liveStreamStatus = true,
            //        latitude,
            //        longitude
            //    };

            //    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            //    {
            //        var metadataBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata));
            //        await _webSocket.SendAsync(new ArraySegment<byte>(metadataBytes), WebSocketMessageType.Text, true, _webSocketCts.Token);

            //        // Send application icon only once after connection
            //        if (!_appLogoSent && appLogoBytes.Length > 0)
            //        {
            //            _appLogoSent = true;

            //            var logoHeader = Encoding.UTF8.GetBytes("{\"type\":\"applogo\"}");
            //            await _webSocket.SendAsync(new ArraySegment<byte>(logoHeader), WebSocketMessageType.Text, true, _webSocketCts.Token);

            //            await _webSocket.SendAsync(new ArraySegment<byte>(appLogoBytes), WebSocketMessageType.Binary, true, _webSocketCts.Token);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error sending WebSocket data: {ex.Message}");
            //    _isWebSocketConnected = false;
            //}
        }

        // Capture application icon bytes
        public byte[] GetApplicationIconBytes(string activeAppName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(activeAppName);
                if (processes.Length > 0)
                {
                    string exePath = processes[0].MainModule.FileName;
                    using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                    {
                        if (icon != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                icon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                return ms.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting application icon: {ex.Message}");
            }

            return Array.Empty<byte>();
        }

        // Send screenshot on request
        private async Task SendScreenshot()
        {
            try
            {
                Console.WriteLine("Preparing to send screenshot...");
                byte[] screenshotData = _screenCaptureTracker.NewCaptureScreen();

                if (_webSocket?.State == WebSocketState.Open && screenshotData.Length > 0)
                {
                    Console.WriteLine($"Sending screenshot ({screenshotData.Length} bytes)");

                    var screenshotHeader = Encoding.UTF8.GetBytes("{\"type\":\"screenshot\"}");
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(screenshotHeader),
                        WebSocketMessageType.Text,
                        true,
                        _webSocketCts.Token);

                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(screenshotData),
                        WebSocketMessageType.Binary,
                        true,
                        _webSocketCts.Token);

                    Console.WriteLine("Screenshot sent successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send screenshot: {ex.Message}");
            }
        }
        // Stop and clean up the WebSocket connection
        public async Task StopWebSocket()
        {
            try
            {
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

                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _webSocketCts.Token);
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending or closing WebSocket: {ex.Message}");
            }
            finally
            {
                _isWebSocketConnected = false;

                _webSocketCts?.Cancel();
                _metadataCts?.Cancel();

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
