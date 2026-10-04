namespace Bikontrol.Domain.Entities
{
    /// <summary>
    /// Append-only trail of who changed what. Written automatically by
    /// <c>AppDbContext.SaveChangesAsync</c> for user-facing entities, so services
    /// never have to remember to log. <see cref="UserId"/> is null for changes
    /// made by the system (background jobs, seeding).
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        /// <summary>Author of the change; null when it comes from a background job.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Entity CLR name, e.g. "Motorcycle".</summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Primary key of the affected row (as text; supports Guid/long).</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Created, Updated or Deleted.</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// JSON with the changed fields only ({ "Name": { "from": ..., "to": ... } })
        /// for updates, or the assigned values for creates/deletes. Null when there
        /// is nothing meaningful to store.
        /// </summary>
        public string? Changes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
