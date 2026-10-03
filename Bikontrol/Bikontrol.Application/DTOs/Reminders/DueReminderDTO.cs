namespace Bikontrol.Application.DTOs.Reminders
{
    /// <summary>Browser Web Push subscription payload (PushSubscription.toJSON()).</summary>
    public class RegisterPushSubscriptionRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public PushSubscriptionKeys Keys { get; set; } = new();
    }

    public class PushSubscriptionKeys
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class UnregisterPushSubscriptionRequest
    {
        public string Endpoint { get; set; } = string.Empty;
    }

    /// <summary>A maintenance the user should attend to soon (or now).</summary>
    public class DueReminderDTO
    {
        public Guid UserMaintenanceId { get; set; }
        public Guid MotorcycleId { get; set; }
        public string MotorcycleName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TrackingType { get; set; } = "Km";
        public int RemainingKm { get; set; }
        public int RemainingDays { get; set; }
        public int LifePercent { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime? LastPerformedAt { get; set; }
        public int? LastPerformedKm { get; set; }
    }
}
