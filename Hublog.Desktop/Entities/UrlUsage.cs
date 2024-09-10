namespace Hublog.Desktop.Entities
{
    public class UrlUsage
    {
        public int UserId { get; set; }
        public string Url { get; set; }
        public string TotalUsage { get; set; }
        public DateTime UsageDate { get; set; }
        public string Details { get; set; }
    }
}
