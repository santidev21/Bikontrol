using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Maintenance
{
    public class FollowDefaultRequest
    {
        public Guid DefaultId { get; set; }
        public int? KmInterval { get; set; }
        public int? TimeIntervalWeeks { get; set; }
        public string TrackingType { get; set; } = "Km";
    }
}
