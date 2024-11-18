namespace Hublog.Desktop.Entities
{
    public class GetModels
    {
        public int OrganizationId { get; set; }
        public int UserId { get; set; }

        public DateTime CDate { get; set; }

        // New properties
        public string startDate { get; set; }
        public string endDate { get; set; }
    }

    public class GetAttendanceDetailsModels
    {
        public int OrganizationId { get; set; }
        public int UserId { get; set; }

        // New properties
        public string startDate { get; set; }
        public string endDate { get; set; }
    }
}
