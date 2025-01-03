using Hublog.Desktop.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Platform;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;

namespace Hublog.Desktop.Components.Pages
{
    public partial class Dashboard
    {
        private const int IdleApiTriggerThreshold = 120000; // 2 min in milliseconds
        private Timer autoInactivityTimer;  // Renamed timer
        private uint _lastInputTime;

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        // Event to handle inactivity
        public event Action OnInactivityDetected;
        public class UserPunchInOutDetails
        {
            public string FirstName { get; set; }
            public string Email { get; set; }
            public string EmployeeId { get; set; }
            public bool Active { get; set; }
            public DateTime AttendanceDate { get; set; }
            public DateTime Start_Time { get; set; }
            public DateTime End_Time { get; set; }
            public TimeSpan Total_Time { get; set; }
            public DateTime Late_Time { get; set; }
            public int Status { get; set; }
        }

        public class BreakDetails
        {
            public string FirstName { get; set; }
            public string Email { get; set; }
            public string breakType { get; set; }
            public DateTime BreakDate { get; set; }
            public DateTime Start_Time { get; set; }
            public DateTime End_Time { get; set; }
            public DateTime? breakDuration { get; set; }
        }

        public class AlertRulesdatas
        {
            public int Id { get; set; }
            public bool break_alert_status { get; set; }
            public int AlertThreshold { get; set; }
            public int PunchoutThreshold { get; set; }
            public bool Status { get; set; }
            public int OrganizationId { get; set; }
        }

        public class UserActivitydatas
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public DateTime TriggeredTime { get; set; }
        }

        public class BreakResponse
        {
            public List<BreakDetails> BreakDetails { get; set; }
        }

        public class AlertRulesResponse
        {
            public List<AlertRulesdatas> AlertRulesdatas { get; set; }
        }

        #region Declares
        private string elapsedTime = "00:00:00";

        public class TimeSpanConverter : JsonConverter<TimeSpan>
        {
            public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                string timeString = reader.Value.ToString();

                if (TimeSpan.TryParseExact(timeString, @"hh\:mm\:ss", null, out TimeSpan result))
                {
                    return result;
                }

                // Manually handle hours exceeding 23
                string[] parts = timeString.Split(':');
                if (parts.Length == 3 &&
                    int.TryParse(parts[0], out int hours) &&
                    int.TryParse(parts[1], out int minutes) &&
                    int.TryParse(parts[2], out int seconds))
                {
                    return new TimeSpan(hours, minutes, seconds);
                }

                throw new FormatException($"Invalid TimeSpan format: {timeString}");
            }

            public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString(@"hh\:mm\:ss"));
            }
        }


        protected override async Task OnInitializedAsync()
        {
            await JSRuntime.InvokeVoidAsync("removeItem", "breakStatus");
            await JSRuntime.InvokeVoidAsync("setItem", "triggerInactiveAlert", "true");
            await JSRuntime.InvokeVoidAsync("closeInactiveModal");
            await JSRuntime.InvokeVoidAsync("closeUpdateModal");
            StopAudioPlaybackLoop();
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

            // Initial network check (optional)
            CheckNetworkStatus();
            isLoading = true;
            // handle previous day attendance
            DateTime currentDate = DateTime.Now; // Get the current date and time
            DateTime dateOneDaysBefore = currentDate.AddDays(-1); // Subtract 1 days
            DateTime dateSixDaysBefore = currentDate.AddDays(-6); // Subtract 5 days
            try
            {
                string URL = $"{MauiProgram.OnlineURL}api/Users/GetUserPunchInOutDetails"
           + $"?OrganizationId={MauiProgram.Loginlist.OrganizationId}"
           + $"&UserId={MauiProgram.Loginlist.Id}"
           + $"&startDate={dateSixDaysBefore:yyyy-MM-dd}"
           + $"&endDate={dateOneDaysBefore:yyyy-MM-dd}";

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                var response = await HttpClient.GetAsync(URL);
                Console.WriteLine("Response Data: " + response);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response
                    var attendanceDetails = JsonConvert.DeserializeObject<List<UserPunchInOutDetails>>(
                        responseData,
                        new JsonSerializerSettings
                        {
                            Converters = new List<JsonConverter> { new TimeSpanConverter() }
                        });

                    if (attendanceDetails != null && attendanceDetails.Count >= 1)
                    {
                        var lastDetail = attendanceDetails.Last();  // Get the last item
                        DateTime lastAttendanceDate = lastDetail.AttendanceDate;
                        DateTime lastStartTime = lastDetail.Start_Time;
                        DateTime lastEndTime = lastDetail.End_Time;

                        // Check if End_Time is "0001-01-01T00:00:00" (DateTime.MinValue)
                        if (lastStartTime != DateTime.MinValue && lastEndTime == DateTime.MinValue)
                        {
                            Console.WriteLine("End_Time is the default value: 0001-01-01T00:00:00");
                            try
                            {
                                string apiUrl = $"{MauiProgram.OnlineURL}api/Users/Get_Active_Time"
                             + $"?userid={MauiProgram.Loginlist.Id}"
                             + $"&startDate={dateSixDaysBefore:yyyy-MM-dd}"
                             + $"&endDate={dateOneDaysBefore:yyyy-MM-dd}";

                                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                                var activityresponse = await HttpClient.GetAsync(apiUrl);
                                Console.WriteLine("Response Data: " + activityresponse);

                                if (activityresponse.IsSuccessStatusCode)
                                {
                                    var activityresponseData = await activityresponse.Content.ReadAsStringAsync();

                                    // Deserialize JSON response
                                    var useractivityList = JsonConvert.DeserializeObject<List<UserActivitydatas>>(activityresponseData);
                                    if (useractivityList != null && useractivityList.Count >= 1)
                                    {
                                        DateTime lastActiveTime = useractivityList[^1].TriggeredTime;
                                        Console.WriteLine(lastActiveTime);


                                        var attendanceModels = new List<UserAttendanceModel>
        {
            new UserAttendanceModel
            {
                Id = 0,
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                AttendanceDate = lastAttendanceDate,
                Start_Time = lastStartTime,
                End_Time = lastActiveTime,
                Late_Time = null,
                Total_Time = null,
                Status = 1,
                Punchout_type="system"
            }
        };

                                        var json = JsonConvert.SerializeObject(attendanceModels);
                                        var content = new StringContent(json, Encoding.UTF8, "application/json");


                                        HttpResponseMessage punchoutresponse;
                                        try
                                        {
                                            punchoutresponse = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/InsertAttendance", content);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error while calling InsertAttendance API: {ex.Message}");
                                            await JSRuntime.InvokeVoidAsync("openErrorModal");
                                            return;
                                        }

                                        var responseString = await punchoutresponse.Content.ReadAsStringAsync();

                                        if (punchoutresponse.IsSuccessStatusCode)
                                        {
                                            buttonText = "Punch In";
                                            punchInTimer?.Dispose();
                                            // handle modals
                                            await JSRuntime.InvokeVoidAsync("closePunchoutConfirmationModal");
                                            await JSRuntime.InvokeVoidAsync("closeInactiveModal");
                                            await JSRuntime.InvokeVoidAsync("setItem", "triggerInactiveAlert", "true");
                                            StopAudioPlaybackLoop();
                                            //

                                            await JSRuntime.InvokeVoidAsync("setItem", "elapsedTime", "00:00:00");
                                            await JSRuntime.InvokeVoidAsync("setItem", "punchInTime", null);
                                            if (_userActivitytimer != null)
                                            {
                                                _userActivitytimer.Dispose();
                                            }

                                        }
                                        else
                                        {
                                            Console.WriteLine("Error: " + punchoutresponse.StatusCode);
                                            await JSRuntime.InvokeVoidAsync("openErrorModal");
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Error: " + activityresponse.StatusCode);
                                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error fetching breaks: {ex.Message}");
                                await JSRuntime.InvokeVoidAsync("openErrorModal");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Last End_Time: " + lastEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No attendance entries found.");
                        // handle modals
                        await JSRuntime.InvokeVoidAsync("closePunchoutConfirmationModal");
                        await JSRuntime.InvokeVoidAsync("closeInactiveModal");
                        await JSRuntime.InvokeVoidAsync("setItem", "triggerInactiveAlert", "true");
                        StopAudioPlaybackLoop();
                        //

                        await JSRuntime.InvokeVoidAsync("setItem", "elapsedTime", "00:00:00");
                        await JSRuntime.InvokeVoidAsync("setItem", "punchInTime", null);

                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching breaks: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("openErrorModal");
            }
            finally
            {
                HandleCurrentAttendance();
            }
        }

        private bool isTimerRunning = false;
        private bool isOnBreak = false;
        private TimeSpan timeSpan = TimeSpan.Zero;

        private System.Threading.Timer punchInTimer;
        private System.Threading.Timer screenshotTimer;

        private string buttonText = "Punch In";
        private string breakButtonText = "Break";
        private int currentType = 0;

        private string firstName;
        private string lastName;
        private string userEmail;
        private string initials;

        private TimeSpan screenshotInterval = TimeSpan.FromMinutes(5);
        private bool isTimerActive = false;
        private bool InactivealertStatus;
        private long InactivityThreshold = 1800000; // 30 min in milliseconds
        private long InactivityAlertThreshold = 600000; // 10 min in milliseconds
        private bool isLoading = false;
        private bool isPunchButtonLoading = false;

        private List<BreakMaster> availableBreaks = new List<BreakMaster>();
        private int selectedBreakId;
        private BreakInfo selectedBreakInfo;

        private Timer breakTimer;
        private TimeSpan remainingTime;
        private bool isBreakActive = false;
        private int _currentBreakEntryId;
        private Timer _userActivitytimer;
        private DateTime _lastApiCallTime = DateTime.MinValue;  // Variable to track the last API call time
        private string lastSynctime;

        [Inject]
        public IScreenCaptureService ScreenCaptureService { get; set; }
        [Inject] public HttpClient HttpClient { get; set; }
        #endregion

        private HttpClient _httpClient;

        public Dashboard()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(MauiProgram.OnlineURL)
            };
        }

        protected override async void OnInitialized()
        {
            base.OnInitialized();
            autoInactivityTimer = new Timer(CheckInactivity, null, TimeSpan.Zero, TimeSpan.FromSeconds(1)); //for auto punchout implementation
            UpdateDateTime();
            StartTimertwo();

            var claimsJson = Preferences.Default.Get("Claim", string.Empty);
            if (!string.IsNullOrEmpty(claimsJson))
            {
                try
                {
                    var claims = JsonConvert.DeserializeObject<Dictionary<string, string>>(claimsJson);
                    if (claims != null)
                    {
                        firstName = claims.ContainsKey("First_Name") ? claims["First_Name"] : "N/A";
                        lastName = claims.ContainsKey("Last_Name") ? claims["Last_Name"] : "N/A";
                        userEmail = claims.ContainsKey("Email") ? claims["Email"] : "N/A";
                        initials = GetInitials(firstName, lastName);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing claims: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No claims found in Preferences.");
            }
        }

        private void ToggleTimer()
        {
            if (isTimerRunning)
            {
                StopTimer();
            }
            else
            {
                if (TimeSpan.TryParse(elapsedTime, out var parsedTimeSpan))
                {
                    timeSpan = parsedTimeSpan;
                }
                StartTimer();
            }
            InvokeAsync(StateHasChanged);
        }
        private async void StartTimer()
        {
            isTimerRunning = true;
            punchInTimer = new System.Threading.Timer(UpdateTimer, null, 0, 1000);
            buttonText = "Punch Out";
            currentType = 1;
            var punchIntimefromLocalStorage = await JSRuntime.InvokeAsync<string>("getpunchInTime");
            Console.WriteLine(punchIntimefromLocalStorage);

            await StartTracking();
            StartScreenshotTimer();
        }
        private async void StopTimer()
        {
            //isTimerRunning = false;
            //punchInTimer?.Dispose();
            //timeSpan = TimeSpan.Zero;
            isPunchButtonLoading = true;
            elapsedTime = "00:00:00";

            isOnBreak = false;
            breakButtonText = "Break";

            if (currentType == 1 || currentType == 2)
            {
                var systemInfoService = new SystemInfoService();
                var systemInfo = systemInfoService.GetSystemInfo();

                var systemInfoModel = new SystemInfoModel
                {
                    UserId = systemInfo.UserId,
                    DeviceId = systemInfo.DeviceId,
                    DeviceName = systemInfo.DeviceName,
                    Platform = systemInfo.Platform,
                    OSName = systemInfo.OSName,
                    OSBuild = systemInfo.OSBuild,
                    SystemType = systemInfo.SystemType,
                    IPAddress = systemInfo.IPAddress,
                    AppType = systemInfo.AppType,
                    HublogVersion = systemInfo.HublogVersion,
                    Status = 0
                };

                try
                {
                    var systemInfoJson = JsonConvert.SerializeObject(systemInfoModel);
                    var systemInfoContent = new StringContent(systemInfoJson, Encoding.UTF8, "application/json");

                    // Send the POST request
                    var systemInfoResponse = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/SystemInfo/InsertOrUpdateSystemInfo", systemInfoContent);

                    // Read the response content
                    var systemInfoResponseString = await systemInfoResponse.Content.ReadAsStringAsync();

                    if (systemInfoResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("System info status updated to offline successfully.");
                        PunchOut("user");
                    }
                    else
                    {
                        await JSRuntime.InvokeVoidAsync("openErrorModal");
                        Console.WriteLine($"Error updating system info status: {systemInfoResponseString}");
                    }
                }
                catch (Exception ex)
                {
                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                ChangeStatus();
            }
        }

        private PeriodicTimer? breakAlerttimer; // Timer for audio playback
        private async void TimerCallback(object state)
        {
            //if (remainingTime.TotalSeconds > 0)
            //{
            //    remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
            //    InvokeAsync(StateHasChanged);
            //}
            //else
            //{
            //    isBreakActive = false;
            //    breakTimer?.Dispose();
            //    InvokeAsync(() =>
            //    {
            //        StateHasChanged();
            //        JSRuntime.InvokeVoidAsync("changeResumeButtonColorToRed");
            //    });
            //}

            if (isBreakActive && remainingTime.TotalSeconds > 0)
            {
                remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
                await JSRuntime.InvokeVoidAsync("setItem", "breakStatus", "isBreaktime");
            }
            else
            {
                // Handle end of break
                if (isBreakActive)
                {
                    isBreakActive = false;
                    await JSRuntime.InvokeVoidAsync("changeResumeButtonColorToRed");
                    var getBreakAlertstatus = await JSRuntime.InvokeAsync<string>("getbreakAlertStatus");
                    if (getBreakAlertstatus == "true")
                    {
                        await PlayAudio();
                        StartAudioPlaybackLoop();
                        await HandleAlertTrigger("Break Time Exceeded");
                    }
                }
                // Possibly reset or stop the timer
            }
        }

        //Break alert timer logic. the alert should play every 20sec
        private void StartAudioPlaybackLoop()
        {
            // Start a new periodic timer
            breakAlerttimer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            // Run the playback loop
            _ = PlayAudioLoopAsync();
        }

        private async void StopAudioPlaybackLoop()
        {
            // Dispose of the timer if it exists
            breakAlerttimer?.Dispose();
            breakAlerttimer = null;
            await PauseAudio();
        }

        private async Task PlayAudioLoopAsync()
        {
            if (breakAlerttimer == null)
                return;

            try
            {
                while (await breakAlerttimer.WaitForNextTickAsync())
                {
                    await PlayAudio();
                }
            }
            catch (OperationCanceledException)
            {
                // Timer was stopped
            }
            finally
            {
                StopAudioPlaybackLoop(); // Clean up
            }
        }
        public async Task PlayAudio()
        {
            await JSRuntime.InvokeVoidAsync("playAudio");

        }

        public async Task PauseAudio()
        {
            await JSRuntime.InvokeVoidAsync("pauseAudio");
        }

        private async void UpdateTimer(object state)
        {
            if (isTimerRunning)
            {
                timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                elapsedTime = timeSpan.ToString(@"hh\:mm\:ss");
                await JSRuntime.InvokeVoidAsync("setItem", "elapsedTime", elapsedTime);
                await InvokeAsync(StateHasChanged);
            }
        }
        private DateTime GetISTTime()
        {
            var utcNow = DateTime.UtcNow;
            var istOffset = TimeSpan.FromHours(5.5);
            return utcNow.Add(istOffset);

        }

        #region Track
        private ApplicationMonitor _monitor;
        private bool isTracking = false;
        private string token;

        private async Task StartTracking()
        {
            isTracking = true;

            var httpClient = MauiProgram.CreateMauiApp().Services.GetRequiredService<HttpClient>();
            httpClient.BaseAddress = new Uri(MauiProgram.OnlineURL);

            _monitor = new ApplicationMonitor(httpClient);

            while (isTracking)
            {
                await _monitor.UpdateApplicationOrUrlUsageAsync(token);
                await Task.Delay(2000);
            }
        }

        private void StopTracking()
        {
            isTracking = false;
        }
        #endregion

        private string GetInitials(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return "N/A";

            return $"{firstName?[0]}{lastName?[0]}".ToUpper();
        }
        private void ChangeStatus()
        {
            if (currentType == 0)
            {
                buttonText = "Punch In";
                breakButtonText = "Break";
            }
            else if (currentType == 1)
            {
                buttonText = "Punch Out";
                breakButtonText = "Break";
            }
            else if (currentType == 2)
            {
                buttonText = "Punch Out";
                //breakButtonText = "Resume";
            }
        }

        #region Break Section
        private void TakeBreak()
        {
            isOnBreak = false;
            currentType = 1;
            if (isOnBreak)
            {
                isOnBreak = false;
                breakButtonText = "Break";
                currentType = 1;
            }
            else
            {
                isOnBreak = true;
                //breakButtonText = "Resume";
                currentType = 2;
                OpenBreakModal();
            }

            ChangeStatus();
            InvokeAsync(StateHasChanged);
        }
        private async Task FetchAvailableBreaks()
        {
            try
            {
                string URL = MauiProgram.OnlineURL + "api/Users/GetAvailableBreak";

                var getModel = new GetModels
                {
                    OrganizationId = MauiProgram.Loginlist.OrganizationId,
                    UserId = MauiProgram.Loginlist.Id,
                    CDate = DateTime.UtcNow
                };

                string jsonData = JsonConvert.SerializeObject(getModel);

                var requestContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);

                var response = await HttpClient.PostAsync(URL, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    availableBreaks = JsonConvert.DeserializeObject<List<BreakMaster>>(responseString);
                }
                else
                {
                    Console.WriteLine("Failed to fetch available breaks.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching breaks: {ex.Message}");
            }
        }
        private async Task PunchBreakIn(int breakEntryId)
        {
            DateTime istTime = GetISTTime();
            int currentType = 1;
            var userBreakList = new List<UserBreakModel>
        {
            new UserBreakModel
            {
                Id = 0,
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                BreakDate = istTime.Date,
                Start_Time = istTime,
                BreakEntryId = breakEntryId,
                End_Time = null,
                Status = currentType
            }
        };

            var jsonString = JsonConvert.SerializeObject(userBreakList);
            var apiUrl = MauiProgram.OnlineURL + "api/Users/InsertBreak";

            var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);

            try
            {
                var response = await HttpClient.PostAsync(apiUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var breakDetails = await GetBreakDetails(breakEntryId);
                    if (breakDetails != null)
                    {
                        _currentBreakEntryId = breakDetails.Id;
                        selectedBreakInfo = breakDetails;
                        StartBreakTimer(selectedBreakInfo.Max_Break_Time);
                        OpenBreakTimerModal();
                    }
                    else
                    {
                        Console.WriteLine("Failed to retrieve break details.");
                    }
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode} - {responseContent}");
                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("openErrorModal");
            }
        }
        private async Task PunchBreakOut()
        {
            if (_currentBreakEntryId == 0)
            {
                Console.WriteLine("No active break found.");
                StopAudioPlaybackLoop();
                await JSRuntime.InvokeVoidAsync("closeBreakTimerModal");
                await JSRuntime.InvokeVoidAsync("setItem", "breakStatus", null);
                return;
            }
            StopAudioPlaybackLoop();
            await JSRuntime.InvokeVoidAsync("setItem", "breakStatus", null);
            DateTime istTime = GetISTTime();
            var breakEndDetails = new List<UserBreakModel>
        {
            new UserBreakModel
            {
                Id = 0,
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                BreakDate = istTime.Date,
                Start_Time = istTime,
                End_Time = istTime,
                BreakEntryId = _currentBreakEntryId,
                Status = 2
            }
        };

            var jsonString = JsonConvert.SerializeObject(breakEndDetails);
            var apiUrl = MauiProgram.OnlineURL + "api/Users/InsertBreak";

            var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);

            try
            {
                var response = await HttpClient.PostAsync(apiUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Break ended successfully.");
                    isBreakActive = false;
                    _currentBreakEntryId = 0;
                    await JSRuntime.InvokeVoidAsync("closeBreakTimerModal");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode} - {responseContent}");
                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("openErrorModal");
            }
        }
        private async Task ResumeWorking()
        {
            await PunchBreakOut();
        }
        private async Task<BreakInfo> GetBreakDetails(int breakEntryId)
        {
            var apiUrl = $"{MauiProgram.OnlineURL}api/Users/GetBreakMasterById?id={breakEntryId}";

            try
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                var response = await HttpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<BreakInfo>(responseString);
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve break details: {response.StatusCode}");
                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving break details: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("openErrorModal");
                return null;
            }
        }
        private void OpenBreakModal()
        {
            FetchAvailableBreaks().ContinueWith(_ =>
            {
                JSRuntime.InvokeVoidAsync("openBreakModal");
                InvokeAsync(StateHasChanged);
            });
        }
        private void OpenBreakTimerModal()
        {
            JSRuntime.InvokeVoidAsync("resetResumeButtonColor");
            JSRuntime.InvokeVoidAsync("openBreakTimerModal");
            InvokeAsync(StateHasChanged);
        }
        private void OnBreakOptionChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value.ToString(), out int breakId))
            {
                selectedBreakId = breakId;
            }
        }
        private async void CloseModal()
        {
            var selectedBreak = availableBreaks.FirstOrDefault(b => b.Id == selectedBreakId);

            if (selectedBreak != null)
            {
                selectedBreakInfo = new BreakInfo
                {
                    Id = selectedBreak.Id,
                    Name = selectedBreak.Name,
                    Max_Break_Time = selectedBreak.Max_Break_Time
                };
                await JSRuntime.InvokeVoidAsync("setItem", "activeBreakId", selectedBreak.Id);
                await PunchBreakIn(selectedBreak.Id).ContinueWith(_ =>
                  {
                      InvokeAsync(StateHasChanged);
                  });
            }
            else
            {
                Console.WriteLine("No break selected or break not found.");
            }

            await JSRuntime.InvokeVoidAsync("closeBreakModal");
        }
        private void StartBreakTimer(int breakDurationMinutes)
        {
            //remainingTime = TimeSpan.FromMinutes(breakDurationMinutes);
            //isBreakActive = true;
            //breakTimer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            if (breakTimer != null)
            {
                breakTimer.Dispose();
            }

            remainingTime = TimeSpan.FromMinutes(breakDurationMinutes);
            isBreakActive = true;
            breakTimer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        #endregion

        #region PunchIn and PunchOut
        private async void PunchIn()
        {
            isPunchButtonLoading = true;
            try
            {
                DateTime istTime = GetISTTime();
                await JSRuntime.InvokeVoidAsync("setItem", "punchInTime", istTime);
                Console.WriteLine("punchin timeee", istTime);

                var attendanceModels = new List<UserAttendanceModel>
        {
            new UserAttendanceModel
            {
                Id = 0,
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                AttendanceDate = istTime.Date,
                Start_Time = istTime,
                End_Time = null,
                Late_Time = null,
                Total_Time = null,
                Status = currentType
            }
        };

                var json = JsonConvert.SerializeObject(attendanceModels);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/InsertAttendance", content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while calling InsertAttendance API: {ex.Message}");
                    HandlePunchInFailure();
                    return;
                }
                finally
                {
                    isPunchButtonLoading = false;
                }

                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (TimeSpan.TryParse(elapsedTime, out var parsedTimeSpan))
                    {
                        timeSpan = parsedTimeSpan;
                    }
                    _userActivitytimer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                    StartTimer();
                    await JSRuntime.InvokeVoidAsync("PunchInAudio");
                    await InvokeAsync(StateHasChanged);

                    var systemInfoService = new SystemInfoService();
                    var systemInfo = systemInfoService.GetSystemInfo();

                    var systemInfoModel = new SystemInfoModel
                    {
                        UserId = systemInfo.UserId,
                        DeviceId = systemInfo.DeviceId,
                        DeviceName = systemInfo.DeviceName,
                        Platform = systemInfo.Platform,
                        OSName = systemInfo.OSName,
                        OSBuild = systemInfo.OSBuild,
                        SystemType = systemInfo.SystemType,
                        IPAddress = systemInfo.IPAddress,
                        AppType = systemInfo.AppType,
                        HublogVersion = systemInfo.HublogVersion,
                        Status = 1
                    };

                    var jsonContent = JsonConvert.SerializeObject(systemInfoModel);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var systemInfoResponse = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/SystemInfo/InsertOrUpdateSystemInfo", httpContent);

                    if (systemInfoResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("System info successfully inserted or updated.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to insert or update system info.");
                    }
                }
                else
                {
                    Console.WriteLine($"Error response from InsertAttendance API: {responseString}");
                    HandlePunchInFailure();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in PunchIn method: {ex.Message}");
                HandlePunchInFailure();
            }
            finally
            {
                isPunchButtonLoading = false;
            }
        }


        private async void HandlePunchInFailure()
        {
            try
            {
                punchInTimer?.Dispose();
                await JSRuntime.InvokeVoidAsync("setItem", "punchInTime", null);
                string savedElapsedTime = await JSRuntime.InvokeAsync<string>("getelapsedTime");

                // Parse the saved elapsed time, if it exists
                if (!string.IsNullOrEmpty(savedElapsedTime) && TimeSpan.TryParse(savedElapsedTime, out var parsedTime))
                {
                    timeSpan = parsedTime;
                    var formatSavedElapsedTime = timeSpan.ToString(@"hh\:mm\:ss");
                    elapsedTime = formatSavedElapsedTime;
                }

                buttonText = "Punch In";
                isTimerRunning = false;
                punchInTimer?.Dispose();
                timeSpan = TimeSpan.Zero;
                isOnBreak = false;
                breakButtonText = "Break";
                await JSRuntime.InvokeVoidAsync("setItem", "elapsedTime", "00:00:00");
                await JSRuntime.InvokeVoidAsync("openErrorModal");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while handling punch-in failure: {ex.Message}");
            }
        }

        public void PunchOutModal()
        {
            JSRuntime.InvokeVoidAsync("openPunchoutConfirmationModal");
        }
        public async void PunchOut(string punchoutType)
        {
            isPunchButtonLoading = true;
            try
            {
                if (currentType == 2)
                {
                    isOnBreak = false;
                    currentType = 1;
                }
                currentType = 1;
                DateTime istTime = GetISTTime();

                // handle modals
                await JSRuntime.InvokeVoidAsync("closePunchoutConfirmationModal");
                await JSRuntime.InvokeVoidAsync("closeInactiveModal");
                await JSRuntime.InvokeVoidAsync("setItem", "triggerInactiveAlert", "true");
                StopAudioPlaybackLoop();
                //

                var punchIntime = await JSRuntime.InvokeAsync<string>("getpunchInTime");
                Console.WriteLine("punchIn time: " + punchIntime);

                // Attempt to parse punchIntime to DateTime
                DateTime? parsedPunchInTime = null;
                if (DateTime.TryParse(punchIntime, out var punchInDateTime))
                {
                    // Convert to UTC if necessary
                    parsedPunchInTime = punchInDateTime.ToUniversalTime();
                }
                else
                {
                    Console.WriteLine("Failed to parse punchIn time.");
                }

                var attendanceModels = new List<UserAttendanceModel>
        {
            new UserAttendanceModel
            {
                Id = 0,
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                AttendanceDate = istTime.Date,
                Start_Time = parsedPunchInTime,
                End_Time = istTime,
                Late_Time = null,
                Total_Time = null,
                Status = currentType,
                Punchout_type= punchoutType
            }
        };

                var json = JsonConvert.SerializeObject(attendanceModels);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/InsertAttendance", content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while calling InsertAttendance API: {ex.Message}");
                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                    return;
                }
                finally
                {
                    isPunchButtonLoading = false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    currentType = 0;
                    buttonText = "Punch In";
                    breakButtonText = "Break";
                    isOnBreak = false;
                    isTimerRunning = false;
                    punchInTimer?.Dispose();
                    timeSpan = TimeSpan.Zero;
                    await JSRuntime.InvokeVoidAsync("PunchOutAudio");
                    await JSRuntime.InvokeVoidAsync("setItem", "punchInTime", null);
                    await JSRuntime.InvokeVoidAsync("setItem", "elapsedTime", "00:00:00");
                    if (_userActivitytimer != null)
                    {
                        _userActivitytimer.Dispose();
                    }
                    try
                    {
                        string URL = $"{MauiProgram.OnlineURL}api/Users/GetUserPunchInOutDetails"
                                     + $"?OrganizationId={MauiProgram.Loginlist.OrganizationId}"
                                     + $"&UserId={MauiProgram.Loginlist.Id}"
                                     + $"&startDate={DateTime.Today:yyyy-MM-dd}"
                                     + $"&endDate={DateTime.Today:yyyy-MM-dd}";

                        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                        var responses = await HttpClient.GetAsync(URL);
                        Console.WriteLine("Response Data: " + responses);

                        if (responses.IsSuccessStatusCode)
                        {
                            var responseData = await responses.Content.ReadAsStringAsync();

                            // Deserialize JSON response
                            var attendanceDetails = JsonConvert.DeserializeObject<List<UserPunchInOutDetails>>(responseData);

                            if (attendanceDetails != null && attendanceDetails.Count >= 1)
                            {
                                // Filter entries with today's date
                                var today = DateTime.Today;
                                var todayEntries = attendanceDetails
                          .Where(entry => entry.AttendanceDate.Date == today)
                          .ToList();

                                // Sum total_Time for entries matching today's date
                                var totalDuration = new TimeSpan();
                                foreach (var entry in todayEntries)
                                {
                                    // Check if Total_Time is a valid TimeSpan
                                    if (entry.Total_Time != TimeSpan.Zero)  // Checking for zero duration instead of DateTime.MinValue
                                    {
                                        totalDuration += entry.Total_Time;  // Add the entry's Total_Time to the total duration
                                    }
                                }

                                // Format total duration to "hh:mm:ss"
                                string totalDurationString = totalDuration.ToString(@"hh\:mm\:ss");

                                Console.WriteLine("Total Time for Today's Entries: " + totalDurationString);
                                elapsedTime = totalDurationString;
                            }
                        }
                        else
                        {
                            var errorResponse = await responses.Content.ReadAsStringAsync();
                            Console.WriteLine($"Error: {errorResponse}");
                            await JSRuntime.InvokeVoidAsync("openErrorModal");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching attendance details: {ex.Message}");
                        await JSRuntime.InvokeVoidAsync("openErrorModal");
                    }
                    finally
                    {
                        await _monitor.LastApplicationOrUrlUsageAsync();
                        await Task.Delay(2000);
                        StopTracking();
                        StopScreenshotTimer();
                    }
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("openErrorModal");
                }
            }
            catch (Exception)
            {
                await JSRuntime.InvokeVoidAsync("openErrorModal");
            }

        }
        #endregion

        private void StartScreenshotTimer()
        {
            if (isTimerActive)
            {
                return;
            }

            screenshotTimer = new System.Threading.Timer(async _ =>
            {
                await CaptureAndUploadScreenshot();
            }, null, TimeSpan.Zero, screenshotInterval);

            isTimerActive = true;
        }
        private void StopScreenshotTimer()
        {
            screenshotTimer?.Dispose();
            isTimerActive = false;
        }
        private async Task CaptureAndUploadScreenshot()
        {
            try
            {
                var screenshotData = ScreenCaptureService.CaptureScreen();

                await UploadScreenshot(screenshotData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screenshot Error: {ex.Message}");
            }
        }
        private async Task UploadScreenshot(byte[] imageData)
        {
            string filename = DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg";
            string URL = MauiProgram.OnlineURL + "api/Users/UploadFile";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URL);
                client.Timeout = TimeSpan.FromMinutes(30);
                client.DefaultRequestHeaders.Add("UId", MauiProgram.Loginlist.Id.ToString());
                client.DefaultRequestHeaders.Add("OId", MauiProgram.Loginlist.OrganizationId.ToString());
                client.DefaultRequestHeaders.Add("SDate", GetISTTime().ToString("yyyy-MM-dd HH:mm:ss"));
                client.DefaultRequestHeaders.Add("SType", "ScreenShots");
                client.DefaultRequestHeaders.Add("Authorization", MauiProgram.token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "file", filename);

                HttpResponseMessage response = await client.PostAsync(URL, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Screenshot uploaded successfully at {DateTime.Now}");
                }
                else
                {
                    Console.WriteLine($"Upload Error: {response.StatusCode}, Details: {responseString}");
                }
            }
        }

        private string CurrentDateTime { get; set; }
        private System.Threading.Timer _timer;

        private void UpdateDateTime()
        {
            CurrentDateTime = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
            InvokeAsync(StateHasChanged); // Refresh UI
        }

        private void StartTimertwo()
        {
            _timer = new Timer(_ => UpdateDateTime(), null, 0, 1000); // Update every second
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        //Auto Punchout implementaion
        public async Task HandleInactivity()
        {
            Console.WriteLine("User has been inactive for 10 min.");
            //Check user is during punchin or not
            var punchIntime = await JSRuntime.InvokeAsync<string>("getpunchInTime");

            if (string.IsNullOrEmpty(punchIntime))
            {
                Console.WriteLine("punchIn time is null or empty.");
            }
            else
            {
                // Call the PunchOut function
                PunchOut("system");
            }
        }
        private async void CheckInactivity(object state)
        {
            var lastInputInfo = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (GetLastInputInfo(ref lastInputInfo))
            {
                var idleTime = Environment.TickCount - lastInputInfo.dwTime;
                Console.WriteLine($"Idle time: {idleTime} ms");
                var getBreakstatus = await JSRuntime.InvokeAsync<string>("getbreakStatus");
                Console.WriteLine(getBreakstatus);

                var getInactivityAlertstatus = await JSRuntime.InvokeAsync<string>("getInactivityAlertStatus");
                Console.WriteLine(getInactivityAlertstatus);

                var gettriggerInactiveAlert = await JSRuntime.InvokeAsync<string>("getTriggerInactiveAlert");
                Console.WriteLine(gettriggerInactiveAlert);

                var punchIntimefromLocalStorage = await JSRuntime.InvokeAsync<string>("getpunchInTime");

                if (punchIntimefromLocalStorage == null || punchIntimefromLocalStorage == "null") return;


                if (idleTime > InactivityThreshold)
                {
                    // Trigger inactivity event and handle navigation
                    if (getBreakstatus == "isBreaktime" || getInactivityAlertstatus != "true")
                    {
                        return;
                    }
                    else
                    {
                        OnInactivityDetected?.Invoke();
                        StopAudioPlaybackLoop();
                        await HandleInactivity();
                    }

                }
                // Check if the system has been inactive more than
                if (idleTime > InactivityAlertThreshold)
                {

                    Console.WriteLine("System inactive detected (no input for more than 10 min).");
                    if (gettriggerInactiveAlert == "true" && getInactivityAlertstatus == "true" && getBreakstatus != "isBreaktime")
                    {
                        await PlayAudio();
                        await JSRuntime.InvokeVoidAsync("setItem", "triggerInactiveAlert", "false");
                        StartAudioPlaybackLoop();
                        await JSRuntime.InvokeVoidAsync("openInactiveModal");
                        await HandleAlertTrigger("Inactivity Exceeded");
                    }
                }

                if (idleTime > IdleApiTriggerThreshold)
                {
                    // Ensure HandleIdletimeApi is called only once every 2 minutes
                    if (_lastApiCallTime == DateTime.MinValue || DateTime.Now.Subtract(_lastApiCallTime).TotalMinutes >= 2)
                    {
                        await HandleIdletimeApi();
                        _lastApiCallTime = DateTime.Now;  // Update the last call time to now
                    }
                }
            }
        }


        public async void InactivityModalClose()
        {
            await JSRuntime.InvokeVoidAsync("closeInactiveModal");
            await JSRuntime.InvokeVoidAsync("setItem", "triggerInactiveAlert", "true");
            StopAudioPlaybackLoop();
        }

        // Dispose of the timer when the component is disposed
        public void autoInactivityDispose()  // Renamed Dispose method
        {
            autoInactivityTimer?.Dispose();
        }

        //Alert trigger api handing
        private async Task HandleAlertTrigger(string value)
        {

            DateTime istTime = GetISTTime();

            var alertTriggerDetails = new List<AlertModel>
        {
            new AlertModel
            {
                UserId = MauiProgram.Loginlist.Id,
                Triggered = value,
                TriggeredTime= istTime
            }
        };

            var json = JsonConvert.SerializeObject(alertTriggerDetails);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Alert/InsertAlert", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Handle success if needed
            }
            else
            {
                Console.WriteLine($"Error: {responseString}");
            }
        }

        //user activity api handleing call this api every 2 mins during punchin
        private async void OnTimerElapsed(object state)
        {
            // Call the function here
            var punchIntimefromLocalStorage = await JSRuntime.InvokeAsync<string>("getpunchInTime");
            var getBreakstatus = await JSRuntime.InvokeAsync<string>("getbreakStatus");
            var getLastsynctime = await JSRuntime.InvokeAsync<string>("getLastsynctime");
            DateTime currentTime = DateTime.Now;

            Console.WriteLine(getLastsynctime);
            // Parse the received date string to a DateTime object
            DateTime lastSyncTime = DateTime.Parse(getLastsynctime);

            // Calculate the difference
            TimeSpan timeDifference = currentTime - lastSyncTime;

            // Display the time difference as "3 minutes ago", "1 hour ago", etc.
            string timeAgo = GetTimeAgo(timeDifference);
            lastSynctime = timeAgo;
            await InvokeAsync(StateHasChanged); // Refresh UI

            if (punchIntimefromLocalStorage == null || punchIntimefromLocalStorage == "null") return;
            await HandleUserActivetime(); //call useractivity api
        }

        private async Task HandleUserActivetime()
        {
            DateTime istTime = GetISTTime();

            // Create a single object instead of a list
            var userActivetimeDetails = new UserActivityModal
            {
                UserId = MauiProgram.Loginlist.Id,
                TriggeredTime = istTime
            };

            // Serialize the object directly
            var json = JsonConvert.SerializeObject(userActivetimeDetails);
            Console.WriteLine($"JSON Payload: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Console.WriteLine($"API URL: {MauiProgram.OnlineURL}api/Users/Insert_Active_Time");

            try
            {
                var response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/Insert_Active_Time", content);
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("User activity logged successfully.");
                }
                else
                {
                    Console.WriteLine($"Error: {responseString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                await UpdateUserAttendance();
            }
        }

        private async Task UpdateUserAttendance()
        {
            DateTime istTime = GetISTTime();

            var updateUserAttendanceDetails = new UpdateUserAttendanceModal
            {
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                Date = istTime
            };
            var json = JsonConvert.SerializeObject(updateUserAttendanceDetails);
            Console.WriteLine($"JSON Payload: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Console.WriteLine($"API URL: {MauiProgram.OnlineURL}api/Users/UpdateUserAttendanceDetails");

            try
            {
                var response = await HttpClient.PutAsync($"{MauiProgram.OnlineURL}api/Users/UpdateUserAttendanceDetails", content);
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseString}");

                if (response.IsSuccessStatusCode)
                {

                }
                else
                {
                    Console.WriteLine($"Error: {responseString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        //send idle time to api handling
        private async Task HandleIdletimeApi()
        {
            DateTime istTime = GetISTTime();

            var idleTimeDetails = new IdletimeModal
            {
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                Ideal_duration = 2,
                Ideal_DateTime = istTime
            };
            var json = JsonConvert.SerializeObject(idleTimeDetails);
            Console.WriteLine($"JSON Payload: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Console.WriteLine($"API URL: {MauiProgram.OnlineURL}api/Users/Insert_IdealActivity");

            try
            {
                var response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/Insert_IdealActivity", content);
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                }
                else
                {
                    Console.WriteLine($"Error: {responseString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        private async void HandleCurrentAttendance()
        {
            try
            {
                string URL = $"{MauiProgram.OnlineURL}api/Users/GetUserPunchInOutDetails"
           + $"?OrganizationId={MauiProgram.Loginlist.OrganizationId}"
           + $"&UserId={MauiProgram.Loginlist.Id}"
           + $"&startDate={DateTime.Today:yyyy-MM-dd}"
           + $"&endDate={DateTime.Today:yyyy-MM-dd}";

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                var response = await HttpClient.GetAsync(URL);
                Console.WriteLine("Response Data: " + response);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response
                    var attendanceDetails = JsonConvert.DeserializeObject<List<UserPunchInOutDetails>>(responseData);
                    if (attendanceDetails != null && attendanceDetails.Count >= 1)
                    {
                        var today = DateTime.Today;
                        DateTime currentTime = DateTime.Now;

                        var lastDetail = attendanceDetails.Last();  // Get the last item
                        DateTime lastAttendanceDate = lastDetail.AttendanceDate;
                        DateTime lastStartTime = lastDetail.Start_Time;
                        DateTime lastEndTime = lastDetail.End_Time;

                        var todayEntries = attendanceDetails
                         .Where(entry => entry.AttendanceDate.Date == today)
                         .ToList();

                        // Sum total_Time for entries matching today's date
                        var totalDuration = new TimeSpan();
                        foreach (var entry in todayEntries)
                        {
                            // Check if Total_Time is a valid TimeSpan
                            if (entry.Total_Time != TimeSpan.Zero)  // Checking for zero duration instead of DateTime.MinValue
                            {
                                totalDuration += entry.Total_Time;  // Add the entry's Total_Time to the total duration
                            }
                        }

                        // Format total duration to "hh:mm:ss"
                        string totalDurationString = totalDuration.ToString(@"hh\:mm\:ss");

                        if (lastStartTime != DateTime.MinValue && lastEndTime == DateTime.MinValue) //check end is null or not
                        {
                            await JSRuntime.InvokeVoidAsync("setItem", "punchInTime", lastStartTime);
                            if (_userActivitytimer != null)
                            {
                                _userActivitytimer.Dispose();
                            }
                            // Calculate the difference between current time and lastStartTime
                            TimeSpan timeDifference = currentTime - lastStartTime;

                            // Parse totalDurationString into a TimeSpan object (assuming it's in "hh:mm:ss" format)
                            TimeSpan totalDurationParsed = TimeSpan.Parse(totalDurationString);

                            // Add timeDifference to totalDurationParsed
                            TimeSpan addtime = totalDurationParsed + timeDifference;

                            // Format elapsedTime as "hh:mm:ss"
                            string elapsedTimeString = addtime.ToString(@"hh\:mm\:ss");

                            // Output the final elapsed time
                            Console.WriteLine("Elapsed Time: " + elapsedTimeString);
                            elapsedTime = elapsedTimeString;
                            _userActivitytimer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

                            if (TimeSpan.TryParse(elapsedTime, out var parsedTimeSpan))
                            {
                                timeSpan = parsedTimeSpan;
                            }
                            StartTimer();
                        }
                        else
                        {
                            elapsedTime = totalDurationString;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching breaks: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("openErrorModal");
                isLoading = false;
            }
            finally
            {
                HandlegetBreakData();
            }
        }

        //getbreak api handleing
        private async void HandlegetBreakData()
        {
            DateTime currentDate = DateTime.Now; // Get the current date and time
            DateTime dateSixDaysBefore = currentDate.AddDays(-6); // Subtract 5 days
            try
            {
                string URL = $"{MauiProgram.OnlineURL}api/Users/GetUserBreakRecordDetails"
           + $"?OrganizationId={MauiProgram.Loginlist.OrganizationId}"
           + $"&UserId={MauiProgram.Loginlist.Id}"
           + $"&startDate={dateSixDaysBefore:yyyy-MM-dd}"
           + $"&endDate={DateTime.Today:yyyy-MM-dd}";

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                var response = await HttpClient.GetAsync(URL);
                Console.WriteLine("Response Data: " + response);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response
                    var breakDetailsList = JsonConvert.DeserializeObject<List<BreakDetails>>(responseData);
                    if (breakDetailsList != null && breakDetailsList.Count > 0)
                    {
                        // Get the last item in the list
                        var lastBreakDetail = breakDetailsList[^1]; // C# 8.0 index from the end operator

                        // Use the last item
                        Console.WriteLine(lastBreakDetail);

                        if (lastBreakDetail.breakDuration == null)
                        {
                            // Calculate elapsed time
                            var startTime = lastBreakDetail.Start_Time;
                            var currentTime = DateTime.Now;
                            var res = currentTime - startTime;

                            // get break details
                            var breakIdString = await JSRuntime.InvokeAsync<string>("getactiveBreakId");
                            int breakId;
                            if (int.TryParse(breakIdString, out breakId))
                            {
                                // Successfully converted to an integer
                                Console.WriteLine($"The break ID is: {breakId}");
                            }
                            var breakDetails = await GetBreakDetails(breakId);

                            var morningBreakDuration = TimeSpan.FromMinutes(breakDetails.Max_Break_Time);

                            // Subtract morning break duration
                            var resulttime = morningBreakDuration - res;

                            // Check if elapsed time exceeds 15 minutes
                            if (res >= morningBreakDuration)
                            {
                                resulttime = TimeSpan.Zero; // Display 00:00:00
                            }

                            // Ensure the time is never negative
                            if (resulttime < TimeSpan.Zero)
                            {
                                resulttime = TimeSpan.Zero;
                            }
                            remainingTime = resulttime < TimeSpan.Zero ? TimeSpan.Zero : resulttime;
                            // Calculate the remaining time in minutes
                            int remainingMinutes = (int)Math.Ceiling(remainingTime.TotalMinutes); // Round up to the nearest whole number

                            // Ensure remaining minutes is not negative
                            if (remainingMinutes < 0)
                            {
                                remainingMinutes = 0;
                            }
                            if (breakDetails != null)
                            {
                                _currentBreakEntryId = breakDetails.Id;
                                selectedBreakInfo = breakDetails;
                                StartBreakTimer(remainingMinutes);
                                OpenBreakTimerModal();
                            }
                            else
                            {
                                Console.WriteLine("Failed to retrieve break details.");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching breaks: {ex.Message}");
            }
            finally
            {
                getAlertRuleData();
            }
        }

        //Alert rule api handling
        private async void getAlertRuleData()
        {
            try
            {
                string URL = $"{MauiProgram.OnlineURL}api/Alert/GetAlertRule"
           + $"?OrganizationId={MauiProgram.Loginlist.OrganizationId}";

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MauiProgram.token);
                var response = await HttpClient.GetAsync(URL);
                Console.WriteLine("Response Data: " + response);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response
                    var alertrulesList = JsonConvert.DeserializeObject<List<AlertRulesdatas>>(responseData);
                    Console.WriteLine(alertrulesList);
                    if (alertrulesList == null || alertrulesList.Count == 0)
                    {
                        Console.WriteLine("No data found for alert rules.");
                        await JSRuntime.InvokeVoidAsync("setItem", "inactivityAlertStatus", "false");
                    }
                    else
                    {
                        foreach (var rule in alertrulesList)
                        {
                            // Convert punchoutThreshold from minutes to milliseconds
                            long alertThresholdInMilliseconds = rule.AlertThreshold * 60 * 1000;
                            long punchoutThresholdInMilliseconds = rule.PunchoutThreshold * 60 * 1000;

                            Console.WriteLine(alertThresholdInMilliseconds);
                            Console.WriteLine(punchoutThresholdInMilliseconds);
                            InactivityAlertThreshold = alertThresholdInMilliseconds;
                            InactivityThreshold = punchoutThresholdInMilliseconds;
                            if (rule.Status == true)
                            {
                                await JSRuntime.InvokeVoidAsync("setItem", "inactivityAlertStatus", "true");
                                InactivealertStatus = true;
                            }
                            else
                            {
                                InactivealertStatus = false;
                                await JSRuntime.InvokeVoidAsync("setItem", "inactivityAlertStatus", "false");
                            }
                            if (rule.break_alert_status == true)
                            {
                                await JSRuntime.InvokeVoidAsync("setItem", "breakAlertStatus", "true");
                            }
                            else
                            {
                                await JSRuntime.InvokeVoidAsync("setItem", "breakAlertStatus", "false");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching breaks: {ex.Message}");
            }
            finally
            {
                await Task.Delay(1000);
                isLoading = false;
                await JSRuntime.InvokeVoidAsync("setItem", "lastsyncTime", DateTime.Now);
                lastSynctime = "0 min ago";
                await InvokeAsync(StateHasChanged); // Refresh UI
            }
        }

        //lastsync time getting
        public static string GetTimeAgo(TimeSpan timeDifference)
        {
            if (timeDifference.TotalSeconds < 60)
            {
                return "0 min ago";
            }
            else if (timeDifference.TotalMinutes < 60)
            {
                return $"{Math.Floor(timeDifference.TotalMinutes)} min ago";
            }
            else if (timeDifference.TotalHours < 24)
            {
                return $"{Math.Floor(timeDifference.TotalHours)} hr ago";
            }
            else if (timeDifference.TotalDays < 7)
            {
                return $"{Math.Floor(timeDifference.TotalDays)} day ago";
            }
            else if (timeDifference.TotalDays < 30)
            {
                return $"{Math.Floor(timeDifference.TotalDays / 7)} weeks ago";
            }
            else if (timeDifference.TotalDays < 365)
            {
                return $"{Math.Floor(timeDifference.TotalDays / 30)} months ago";
            }
            else
            {
                return $"{Math.Floor(timeDifference.TotalDays / 365)} years ago";
            }
        }
        //Network checking
        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            // This is where you can handle network changes
            CheckNetworkStatus();
        }
        [Inject] private NavigationManager NavigationManager { get; set; }
        private async void CheckNetworkStatus()
        {
            var currentNetworkStatus = Connectivity.NetworkAccess;

            var currentUri = NavigationManager?.Uri;

            // Check if the current page is the home page
            if (currentUri != null && currentUri.EndsWith("/Dashboard", StringComparison.OrdinalIgnoreCase))
            {
                if (currentNetworkStatus == NetworkAccess.Internet)
                {
                    Console.WriteLine("Device is connected to the internet.");
                    await JSRuntime.InvokeVoidAsync("closeNetworkModal");
                }
                else
                {
                    Console.WriteLine("Device is not connected to the internet.");
                    await Task.Delay(500); // Ensure DOM is rendered
                    await JSRuntime.InvokeVoidAsync("openNetworkModal");
                }
            }
        }
    }
}
