namespace Hublog.Desktop.Entities
{
    public class AlertModel
    {
        public int UserId { get; set; } // Matches "userId"
        public string Triggered { get; set; } // Matches "triggered"
        public DateTime TriggeredTime { get; set; } // Matches "triggeredTime"
    }

}
