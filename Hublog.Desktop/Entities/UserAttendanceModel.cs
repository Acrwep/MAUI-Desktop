﻿namespace Hublog.Desktop.Entities
{
    public class UserAttendanceModel
    {
        public int OrganizationId { get; set; } //(int, not null)
        public int Id { get; set; } //(int, not null)
        public int UserId { get; set; } //(int, not null)
        public DateTime AttendanceDate { get; set; } //(datetime, not null)
        public Nullable<DateTime> Start_Time { get; set; } //(datetime,  null)
        public Nullable<DateTime> End_Time { get; set; } //(datetime,  null)
        public Nullable<DateTime> Total_Time { get; set; } //(datetime,  null)
        public Nullable<DateTime> Late_Time { get; set; } //(datetime,  null)
        public int Status { get; set; } //(int, not null)
    }
}
