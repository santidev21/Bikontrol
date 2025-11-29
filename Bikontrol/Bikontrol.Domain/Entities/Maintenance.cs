using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Domain.Entities
{
    public class Maintenance
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? DefaultKmInterval { get; set; }

        public int? DefaultTimeIntervalWeeks { get; set; }

        public bool IsEnabled { get; set; } = true;

        public ICollection<UserMaintenance> UserMaintenanceTypes { get; set; } = new List<UserMaintenance>();
    }
}
