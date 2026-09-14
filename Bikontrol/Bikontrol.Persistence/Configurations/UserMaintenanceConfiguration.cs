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
            builder.ToTable("UserMaintenanceTypes", t => t.HasCheckConstraint(
                "CK_UserMaintenanceTypes_PositiveInterval",
                "(\"TrackingType\" = 'Km' AND \"KmInterval\" IS NOT NULL AND \"KmInterval\" > 0) OR (\"TrackingType\" = 'Time' AND \"TimeIntervalWeeks\" IS NOT NULL AND \"TimeIntervalWeeks\" > 0)"));

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

            // Concurrencia optimista con xmin (columna de sistema de Postgres).
            // Se usa el API específico de Npgsql porque IsRowVersion() estándar
            // crearía una columna física "xmin" que choca con la del sistema.
#pragma warning disable CS0618 // UseXminAsConcurrencyToken es obsoleto pero mapea la columna real del sistema
            builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

            builder.Property(x => x.MotorcycleId)
                .IsRequired();

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

            builder
                .HasOne(x => x.Motorcycle)
                .WithMany(x => x.UserMaintenances)
                .HasForeignKey(x => x.MotorcycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.MotorcycleId, x.Name });
        }
    }
}
