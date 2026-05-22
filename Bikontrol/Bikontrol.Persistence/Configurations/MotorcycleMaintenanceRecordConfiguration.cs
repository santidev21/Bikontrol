using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikontrol.Persistence.Configurations
{
    public class MotorcycleMaintenanceRecordConfiguration : IEntityTypeConfiguration<MotorcycleMaintenanceRecord>
    {
        public void Configure(EntityTypeBuilder<MotorcycleMaintenanceRecord> builder)
        {
            builder.ToTable("MotorcycleMaintenanceRecords");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PerformedAt).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.PerformedKm).IsRequired(false);

            builder.HasOne(x => x.Motorcycle)
                .WithMany(x => x.MaintenanceRecords)
                .HasForeignKey(x => x.MotorcycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.UserMaintenance)
                .WithMany(x => x.MaintenanceRecords)
                .HasForeignKey(x => x.UserMaintenanceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.MotorcycleId, x.PerformedAt });
            builder.HasIndex(x => new { x.UserMaintenanceId, x.PerformedAt });
        }
    }
}
