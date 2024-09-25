using Hublog.Desktop.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Hublog.Desktop
{
    public class SystemInfoService
    {
        public SystemInfoModel GetSystemInfo()
        {
            var userId = GetUserIdFromToken(MauiProgram.token);

            var systemInfo = new SystemInfoModel
            {
                UserId = userId,
                DeviceName = GetDeviceName(),
                DeviceID = DeviceInfo.Idiom.ToString(),
                Platform = GetPlatform(),
                OSName = GetOSBuild(),
                OSBuild = GetOSName(),
                SystemType = GetSystemType(),
                IPAddress = GetIPAddress(),
                AppType = GetAppType(),
                HublogVersion = GetApplicationVersion()
            };

            return systemInfo;
        }

        private int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        public string GetDeviceName()
        {
            return DeviceInfo.Name;
        }

        public string GetPlatform()
        {
            return DeviceInfo.Platform.ToString();
        }

        public string GetOSName()
        {
            return DeviceInfo.VersionString;
        }

        public string GetAppType()
        {
            return DeviceInfo.Idiom.ToString();
        }

        public string GetOSBuild()
        {
            return RuntimeInformation.OSDescription;
        }

        public string GetSystemType()
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
        }

        public string GetIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "N/A";
        }

        private string GetApplicationVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? version.ToString() : "Unknown Version";
        }
    }
}
