namespace Bikontrol.Application.DTOs.Motorcycle
{
    /// <summary>
    /// The "maintenance book" for one motorcycle: its identity, current km and
    /// every maintenance performed, ready to render as PDF or CSV. This is the
    /// shareable record a rider shows when selling the bike or to a mechanic.
    /// </summary>
    public class MaintenanceBookDTO
    {
        public Guid MotorcycleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int Displacement { get; set; }
        public string Plate { get; set; } = string.Empty;
        public int CurrentKm { get; set; }

        /// <summary>Date of the earliest km record, if any.</summary>
        public DateTime? RidingSince { get; set; }

        public List<MaintenanceBookEntryDTO> Entries { get; set; } = new();

        public int TotalMaintenances => Entries.Count;
        public int? FirstKm => Entries.Where(e => e.PerformedKm.HasValue).Select(e => e.PerformedKm).Min();
    }

    public class MaintenanceBookEntryDTO
    {
        public DateTime PerformedAt { get; set; }
        public int? PerformedKm { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TrackingType { get; set; } = "Km";
        public int? KmInterval { get; set; }
        public int? TimeIntervalWeeks { get; set; }
    }
}
