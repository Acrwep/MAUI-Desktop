using Hublog.Desktop.Entities;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hublog.Desktop.Components.Pages
{
    public partial class Login
    {
        private LoginModels loginModel = new LoginModels();
        private bool isLoggedIn = false;
        private bool isPopupVisible = false;

        private string userNameError = "";
        private string passwordError = "";
        private string generalError = "";

        #region HandleLogin
        private async Task HandleLogin()
        {
            ClearErrorMessages();
            try
            {
                var requestContent = JsonContent.Create(loginModel);

                var response = await Http.PostAsync($"{MauiProgram.OnlineURL}api/Login/UserLogin", requestContent);

                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResult = JsonSerializer.Deserialize<Loginresult>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (loginResult != null)
                    {
                        MauiProgram.Loginlist = loginResult.user;
                        MauiProgram.token = loginResult.token;

                        var claims = new Dictionary<string, string>
                {
                    { "First_Name", loginResult.user.First_Name },
                    { "Last_Name", loginResult.user.Last_Name },
                    { "Email", loginResult.user.Email },
                            { "UserId", loginResult.user.Id.ToString()}
                };

                        var claimsJson = JsonSerializer.Serialize(claims);
                        Preferences.Default.Set("Claim", claimsJson);

                        isLoggedIn = true;

                        Navigation.NavigateTo("/Dashboard");
                    }
                }
                else
                {
                    //errorMessage = responseString;
                    //ShowPopup();

                    if (responseString.Contains("Invalid UserName"))
                    {
                        userNameError = "Invalid username. Please try again.";
                    }
                    else if (responseString.Contains("Invalid Password"))
                    {
                        passwordError = "Invalid password. Please try again.";
                    }
                    else
                    {
                        generalError = "Login failed. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                generalError = $"An unexpected error occurred: {ex.Message}";
                //errorMessage = $"An unexpected error occurred: {ex.Message}";
                //ShowPopup();
                //Console.WriteLine(ex);
                //await JSRuntime.InvokeVoidAsync("alert", $"An unexpected error occurred: {ex.Message}");
            }
        }
        #endregion

        #region Logout
        private async Task Logout()
        {
            var logoutModel = new LoginModels
            {
                UserName = loginModel.UserName,
                Password = loginModel.Password
            };

            try
            {
                var requestContent = JsonContent.Create(logoutModel);

                var response = await Http.PostAsync($"{MauiProgram.OnlineURL}api/Login/UserLogout", requestContent);

                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    LogoutProcess();
                }
                else
                {
                    var errorResult = JsonSerializer.Deserialize<dynamic>(responseString);
                    var errorMessage = (string)errorResult?.message ?? "Logout failed. Please try again.";
                    await JSRuntime.InvokeVoidAsync("alert", errorMessage);
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"An unexpected error occurred: {ex.Message}");
            }
        }
        #endregion

        #region LogoutProcess
        private void LogoutProcess()
        {
            MauiProgram.Loginlist = new Users();
            MauiProgram.token = "";

            Navigation.NavigateTo("/");

            try
            {
                Preferences.Default.Set("UserLoginData", string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing local storage: {ex.Message}");
            }
        }
        #endregion

        #region ClearErrorMessages
        private void ClearErrorMessages()
        {
            userNameError = "";
            passwordError = "";
            generalError = "";
        }
        #endregion
    }
}