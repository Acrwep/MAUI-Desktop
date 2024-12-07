namespace Hublog.Desktop.Entities
{
    internal class UserActivityModal
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Matches "userId"
        public DateTime TriggeredTime { get; set; } // Matches "triggeredTime"
    }
}
