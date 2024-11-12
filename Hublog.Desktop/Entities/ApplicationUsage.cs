namespace Hublog.Desktop.Entities
{
    public class ApplicationUsage
    {
        public int UserId { get; set; }
        public string ApplicationName { get; set; }
        public string TotalUsage { get; set; }
        public DateTime UsageDate { get; set; }
        public string Details { get; set; }
    }
}
