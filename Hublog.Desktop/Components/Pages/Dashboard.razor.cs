using Hublog.Desktop.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Hublog.Desktop.Components.Pages
{
    public partial class Dashboard
    {
        #region Declares
        private string elapsedTime = "00:00:00";
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

        private List<BreakMaster> availableBreaks = new List<BreakMaster>();
        private int selectedBreakId;
        private BreakInfo selectedBreakInfo;

        private Timer breakTimer;
        private TimeSpan remainingTime;
        private bool isBreakActive = false;
        private int _currentBreakEntryId;

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
                await Task.Delay(10000);
            }
        }

        private void StopTracking()
        {
            isTracking = false;
        }
        #endregion

        protected override void OnInitialized()
        {
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
        }
        private async Task PunchBreakOut()
        {
            if (_currentBreakEntryId == 0)
            {
                Console.WriteLine("No active break found.");
                return;
            }

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
                    await JSRuntime.InvokeVoidAsync("closeBreakTimerModal");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
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
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving break details: {ex.Message}");
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
        private void CloseModal()
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

                PunchBreakIn(selectedBreak.Id).ContinueWith(_ =>
                {
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                Console.WriteLine("No break selected or break not found.");
            }

            JSRuntime.InvokeVoidAsync("closeBreakModal");
        }
        private void StartBreakTimer(int breakDurationMinutes)
        {
            remainingTime = TimeSpan.FromMinutes(breakDurationMinutes);
            isBreakActive = true;
            breakTimer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        #endregion

        #region PunchIn and PunchOut
        private async void PunchIn()
        {
            DateTime istTime = GetISTTime();
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

            var response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/InsertAttendance", content);
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
        public async void PunchOut()
        {
            if (currentType == 2)
            {
                isOnBreak = false;
                currentType = 1;
            }
            currentType = 1;
            DateTime istTime = GetISTTime();
            var attendanceModels = new List<UserAttendanceModel>
        {
            new UserAttendanceModel
            {
                Id = 0,
                UserId = MauiProgram.Loginlist.Id,
                OrganizationId = MauiProgram.Loginlist.OrganizationId,
                AttendanceDate = istTime.Date,
                Start_Time = null,
                End_Time = istTime,
                Late_Time = null,
                Total_Time = null,
                Status = currentType
            }
        };

            var json = JsonConvert.SerializeObject(attendanceModels);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            //var response = await HttpClient.PostAsync($"{MauiProgram.OnlineURL}api/Users/InsertAttendance", content);
            var response = await _httpClient.PostAsync($"api/Users/InsertAttendance", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                currentType = 0;
                buttonText = "Punch In";
                breakButtonText = "Break";
                isOnBreak = false;
                punchInTimer?.Dispose();
            }
            else
            {
                Console.WriteLine($"Error: {responseString}");
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
        private void ToggleTimer()
        {
            if (isTimerRunning)
            {
                StopTimer();
                buttonText = "Punch In";
            }
            else
            {
                StartTimer();
            }
            InvokeAsync(StateHasChanged);
        }
        private void StartTimer()
        {
            isTimerRunning = true;
            punchInTimer = new System.Threading.Timer(UpdateTimer, null, 0, 1000);
            buttonText = "Punch Out";
            currentType = 1;
            PunchIn();
            StartTracking();
            StartScreenshotTimer();
        }
        private void StopTimer()
        {
            isTimerRunning = false;
            punchInTimer?.Dispose();
            timeSpan = TimeSpan.Zero;
            elapsedTime = "00:00:00";

            isOnBreak = false;
            breakButtonText = "Break";

            if (currentType == 1 || currentType == 2)
            {
                PunchOut();
                StopTracking();
                StopScreenshotTimer();
            }
            else
            {
                ChangeStatus();
            }
        }
        private void TimerCallback(object state)
        {
            if (remainingTime.TotalSeconds > 0)
            {
                remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
                InvokeAsync(StateHasChanged);
            }
            else
            {
                isBreakActive = false;
                breakTimer?.Dispose();
                InvokeAsync(() =>
                {
                    StateHasChanged();
                    JSRuntime.InvokeVoidAsync("changeResumeButtonColorToRed");
                });
            }
        }
        private void UpdateTimer(object state)
        {
            if (isTimerRunning)
            {
                timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                elapsedTime = timeSpan.ToString(@"hh\:mm\:ss");
                InvokeAsync(StateHasChanged);
            }
        }
        private DateTime GetISTTime()
        {
            var utcNow = DateTime.UtcNow;
            var istOffset = TimeSpan.FromHours(5.5);
            return utcNow.Add(istOffset);

        }
    }
}
