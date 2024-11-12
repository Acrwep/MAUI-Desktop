namespace Hublog.Desktop.Entities
{
    public class BreakMaster
    {
        public int Id { get; set; } //(int, not null)
        public string Name { get; set; } //(varchar(100), not null)
        public int Max_Break_Time { get; set; } //(int, not null)
        public bool Active { get; set; } //(bit, not null)
        public int OrganizationId { get; set; } //(int, not null)

        public static implicit operator List<object>(BreakMaster v)
        {
            throw new NotImplementedException();
        }
    }
}
