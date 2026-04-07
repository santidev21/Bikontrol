using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Domain.Entities
{
    public class MotorcycleKmHistory
    {
        public Guid Id { get; set; }

        public Guid MotorcycleId { get; set; }

        public int Km { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public Motorcycle Motorcycle { get; set; } = null!;
    }
}
