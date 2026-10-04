using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikontrol.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(20);
            builder.Property(a => a.Changes).HasColumnType("jsonb");
            builder.Property(a => a.CreatedAt).IsRequired();

            // Queries are almost always "recent activity for a user" or
            // "history of one row", so index both directions.
            builder.HasIndex(a => new { a.UserId, a.CreatedAt });
            builder.HasIndex(a => new { a.EntityName, a.EntityId, a.CreatedAt });
        }
    }
}
