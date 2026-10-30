using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikontrol.Persistence.Configurations
{
    public class MaintenanceRecordAttachmentConfiguration : IEntityTypeConfiguration<MaintenanceRecordAttachment>
    {
        public void Configure(EntityTypeBuilder<MaintenanceRecordAttachment> builder)
        {
            builder.ToTable("maintenance_record_attachments");
            builder.HasKey(a => a.Id);

            // Legacy rows keep the data URL here (text: no length cap); new rows leave it
            // empty and use StorageKey. Size is enforced by ImageDataUrlValidator (<= ~1 MB).
            builder.Property(a => a.DataUrl).IsRequired();
            builder.Property(a => a.StorageKey).HasMaxLength(300);
            builder.Property(a => a.SizeBytes).IsRequired().HasDefaultValue(0);
            builder.Property(a => a.FileName).HasMaxLength(200);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(50);
            builder.Property(a => a.CreatedAt).IsRequired();

            builder.HasIndex(a => a.MotorcycleMaintenanceRecordId);

            builder.HasOne(a => a.Record)
                .WithMany(r => r.Attachments)
                .HasForeignKey(a => a.MotorcycleMaintenanceRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
