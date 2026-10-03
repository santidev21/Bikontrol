using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikontrol.Persistence.Configurations
{
    public class ReminderLogConfiguration : IEntityTypeConfiguration<ReminderLog>
    {
        public void Configure(EntityTypeBuilder<ReminderLog> builder)
        {
            builder.ToTable("reminder_logs");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Kind).IsRequired().HasMaxLength(30);
            builder.Property(r => r.Channel).IsRequired().HasMaxLength(20);
            builder.Property(r => r.CreatedAt).IsRequired();

            builder.HasIndex(r => new { r.UserMaintenanceId, r.Kind, r.CreatedAt });
            builder.HasIndex(r => r.UserId);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.UserMaintenance)
                .WithMany()
                .HasForeignKey(r => r.UserMaintenanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
