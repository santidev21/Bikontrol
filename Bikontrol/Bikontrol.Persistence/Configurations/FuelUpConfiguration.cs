using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikontrol.Persistence.Configurations
{
    public class FuelUpConfiguration : IEntityTypeConfiguration<FuelUp>
    {
        public void Configure(EntityTypeBuilder<FuelUp> builder)
        {
            builder.ToTable("FuelUps", t =>
            {
                t.HasCheckConstraint(
                    "CK_FuelUps_OdometerKm_NonNegative",
                    "\"OdometerKm\" >= 0");
                t.HasCheckConstraint(
                    "CK_FuelUps_Litres_Positive",
                    "\"Litres\" > 0");
                t.HasCheckConstraint(
                    "CK_FuelUps_TotalCost_NonNegative",
                    "\"TotalCost\" >= 0");
            });

            builder.HasKey(x => x.Id);

            // Optimistic concurrency via xmin (Postgres system column); see UserConfiguration.
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .HasColumnName("xmin")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(x => x.FueledAt).IsRequired();
            builder.Property(x => x.OdometerKm).IsRequired();
            builder.Property(x => x.Litres).HasPrecision(8, 3).IsRequired();
            builder.Property(x => x.TotalCost).HasPrecision(12, 2).IsRequired();
            builder.Property(x => x.IsFullTank).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.Station).HasMaxLength(120);
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.Property(x => x.CreatedAt).IsRequired();

            // PricePerLitre is derived (get-only) and must not become a column.
            builder.Ignore(x => x.PricePerLitre);

            builder.HasOne(x => x.Motorcycle)
                .WithMany(m => m.FuelUps)
                .HasForeignKey(x => x.MotorcycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.MotorcycleId, x.FueledAt });
            builder.HasIndex(x => new { x.MotorcycleId, x.OdometerKm });
        }
    }
}
