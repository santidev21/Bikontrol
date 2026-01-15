using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Configurations
{
    public class UserMaintenanceConfiguration : IEntityTypeConfiguration<UserMaintenance>
    {
        public void Configure(EntityTypeBuilder<UserMaintenance> builder)
        {
            builder.ToTable("UserMaintenanceTypes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Description)
                .HasColumnType("text");

            builder.Property(x => x.KmInterval);

            builder.Property(x => x.TimeIntervalWeeks);

            builder.Property(x => x.TrackingType)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("Km");

            builder.Property(x => x.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(x => x.BaseType)
                .WithMany(x => x.UserMaintenanceTypes)
                .HasForeignKey(x => x.BaseTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
