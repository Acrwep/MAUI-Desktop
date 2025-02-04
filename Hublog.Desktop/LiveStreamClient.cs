using Microsoft.AspNetCore.SignalR.Client;
using Hublog.Desktop.Entities;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Diagnostics; // For debugging purposes

namespace Hublog.Desktop
{
    public class LiveStreamClient
    {
        private HubConnection _connection;
        private readonly IActiveWindowTracker _activeWindowTracker;

        public LiveStreamClient(IActiveWindowTracker activeWindowTracker)
        {
            _activeWindowTracker = activeWindowTracker;

            // Configure the connection to the SignalR hub
            _connection = new HubConnectionBuilder()
                .WithUrl($"{MauiProgram.OnlineURL}livestreamHub")  // Replace with your actual API URL
                .WithAutomaticReconnect()
                .Build();
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
            if (appOrUrl == null || appOrUrl == "")
            {
                return "";
            }
            var parts = appOrUrl.Split(':');
            return parts.Length > 1 ? parts[1].Trim() : string.Empty;
        }
        private bool FinalApplicationNameValidation(string applicationName)
        {
            // Remove all non-alphabetic characters and count only alphabetic letters
            var letterCount = applicationName.Count(char.IsLetter);

            // Check if there are at least two alphabetic letters
            return letterCount >= 2;
        }

        private static bool FinalUrlValidation(string appOrUrl)
        {
            return appOrUrl.Contains(".com") || appOrUrl.Contains(".net") || appOrUrl.Contains(".org") || appOrUrl.Contains(".ai") || appOrUrl.Contains(".in") || appOrUrl.Contains("localhost");
        }
        public async Task StartSignalR()
        {
            try
            {
                // Start the SignalR connection
                await _connection.StartAsync();
                Console.WriteLine("Connected to SignalR Hub");

                // Listen for the "ReceiveLiveData" event from the SignalR server
                _connection.On<string, string, string, string, bool, string>("ReceiveLiveData", (userId, organizationId, activeApp, activeUrl, liveStreamStatus, activeAppLogo) =>
                {
                    // This will be triggered when the server sends data
                    Console.WriteLine($"Received data in client: {userId}, {organizationId}, {activeApp}");
                });

                // Send data continuously (the existing logic)
                while (true)
                {
                    string activeAppandUrl = string.Empty;

                    //app variables
                    string activeApplogo = string.Empty;
                    string activeApp = string.Empty;
                    bool validateActiveApp = false;

                    //url variables
                    string activeUrl = string.Empty;
                    bool validateActiveUrl = false;
                    // Platform-specific code to track the active app and URL
#if WINDOWS
                    activeAppandUrl = _activeWindowTracker.GetActiveWindowTitle();

                    //validate app
                    activeApp = ExtractApplicationName(activeAppandUrl);
                    validateActiveApp = FinalApplicationNameValidation(activeApp);


                    //validate url
                    activeUrl = ExtractUrl(activeAppandUrl);
                    validateActiveUrl = FinalUrlValidation(activeUrl);

                    //app logo validate
                    if (validateActiveApp == true)
                    {
                        activeApplogo = _activeWindowTracker.GetApplicationIconBase64(activeApp);
                    }
                    else
                    {
                        activeApplogo = "";
                    }
                    Console.WriteLine(activeApplogo);
#elif ANDROID
                    activeAppandUrl = "Unknown";  // Or a way to get the active app on Android
#elif IOS
            activeAppandUrl = "Unknown";  // Or a way to get the active app on iOS
#else
            activeAppandUrl = "Unknown";  // Default for non-Windows platforms
#endif

                    // Create the payload object
                    var payload = new
                    {
                        userId = MauiProgram.Loginlist.Id, // Replace with actual user ID
                        organizationId = MauiProgram.Loginlist.OrganizationId, // Replace with actual organization ID
                        activeApp = validateActiveApp == true ? activeApp : "",
                        activeUrl = validateActiveUrl == true ? activeUrl : "",
                        liveStreamStatus = true,
                        activeAppLogo = activeApplogo,
                    };

                    // Send the active data to SignalR Hub
                    await _connection.SendAsync("SendLiveData", payload);

                    // Delay to send data at intervals (1 second in this case)
                    await Task.Delay(1000); // Send data every second
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting connection: {ex.Message}");
                if (ex.Message == "The HubConnection cannot be started if it is not in the Disconnected state.")
                {
                    await _connection.StopAsync();
                }
            }
        }


        public async Task StopSignalR()
        {
            var payload = new
            {
                userId = MauiProgram.Loginlist.Id, // Replace with actual user ID
                organizationId = MauiProgram.Loginlist.OrganizationId, // Replace with actual organization ID
                activeApp = "",
                activeUrl = "",
                liveStreamStatus = false,
                activeAppLogo = "",
            };

            // Send the active data to SignalR Hub
            await _connection.SendAsync("SendLiveData", payload);

            await Task.Delay(1000); // Send data every second
            await _connection.StopAsync();
        }
    }
}