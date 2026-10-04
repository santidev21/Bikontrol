namespace Bikontrol.Application.DTOs.Audit
{
    /// <summary>One entry of the current user's activity trail.</summary>
    public class AuditLogDTO
    {
        public long Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Changes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
