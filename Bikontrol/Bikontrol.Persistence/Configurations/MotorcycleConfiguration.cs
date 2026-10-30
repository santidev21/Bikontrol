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
    public class MotorcycleConfiguration : IEntityTypeConfiguration<Motorcycle>
    {
        public void Configure(EntityTypeBuilder<Motorcycle> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Brand).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Plate).IsRequired().HasMaxLength(20);
            builder.Property(m => m.Image).HasDefaultValue("default.png");
            builder.Property(m => m.ImageKey).HasMaxLength(300);
            builder.Property(m => m.ImageVersion).HasMaxLength(32);
            builder.Property(m => m.ImageSizeBytes).IsRequired().HasDefaultValue(0);

            // Optimistic concurrency via xmin (Postgres system column): two
            // simultaneous writes to the same motorcycle are not silently lost.
            // Npgsql 9 removed UseXminAsConcurrencyToken(); mapping the shadow
            // property explicitly keeps xmin bound to the system column instead
            // of creating a physical "xmin" column.
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .HasColumnName("xmin")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.HasOne(m => m.User)
                   .WithMany(u => u.Motorcycles)
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.UserMaintenances)
                .WithOne(um => um.Motorcycle)
                .HasForeignKey(um => um.MotorcycleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
