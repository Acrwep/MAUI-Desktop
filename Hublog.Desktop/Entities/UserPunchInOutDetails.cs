using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hublog.Desktop.Entities
{
    internal class UserPunchInOutDetails
    {
        public int UserId { get; set; } // Matches "userId"
        public int OrganizationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
