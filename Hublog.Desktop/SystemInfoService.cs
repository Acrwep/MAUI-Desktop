using Hublog.Desktop.Entities;
using Microsoft.Win32;
using System.IdentityModel.Tokens.Jwt;
using System.Management;
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
                DeviceId = GetDeviceId(), //DeviceInfo.Idiom.ToString(),
                Platform = GetPlatform(),
                OSName = GetOSBuild(),
                OSBuild = GetOSName(),
                SystemType = GetSystemType(),
                IPAddress = GetIPAddress(),
                AppType = GetAppType(),
                HublogVersion = "1.2.1"
            }; 

            return systemInfo;
        }

        public string GetDeviceId()
        {
            string deviceId = "N/A";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        deviceId = obj["UUID"]?.ToString();
                        break; 
                    }
                }
            }
            catch (Exception ex)
            {
                deviceId = "Error: " + ex.Message;
            }

            return deviceId;
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
    }
}
