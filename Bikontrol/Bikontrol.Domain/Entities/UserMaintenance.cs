using Bikontrol.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Domain.Entities
{
    public class UserMaintenance
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid? BaseTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? KmInterval { get; set; }

        public int? TimeIntervalWeeks { get; set; }

        public string TrackingType { get; set; } = "Km";

        public bool IsEnabled { get; set; } = true;

        public Maintenance? BaseType { get; set; }
        public User? User { get; set; }
    }
}
