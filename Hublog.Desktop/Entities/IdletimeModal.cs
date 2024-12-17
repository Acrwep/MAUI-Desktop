namespace Hublog.Desktop.Entities
{
    internal class IdletimeModal
    {
        public int UserId { get; set; } // Matches "userId"
        public int OrganizationId { get; set; } // Matches "orgId"
        public int IdealTime { get; set; }
        public int Ideal_duration { get; set; }
        public DateTime Ideal_DateTime { get; set; }

    }
}
