using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Domain.Entities
{
    public class MaintenanceType
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? DefaultKmInterval { get; set; }

        public int? DefaultTimeIntervalWeeks { get; set; }

        public bool IsEnabled { get; set; } = true;

        public ICollection<UserMaintenanceType> UserMaintenanceTypes { get; set; } = new List<UserMaintenanceType>();
    }
}
