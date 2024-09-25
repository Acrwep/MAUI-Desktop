﻿namespace Hublog.Desktop.Entities
{
    public class SystemInfoModel
    {
        public int UserId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceID { get; set; }
        public string Platform { get; set; }
        public string OSName { get; set; }
        public string OSBuild { get; set; }
        public string SystemType { get; set; }
        public string IPAddress { get; set; }
        public string AppType { get; set; }
        public string HublogVersion { get; set; }
    }
}
