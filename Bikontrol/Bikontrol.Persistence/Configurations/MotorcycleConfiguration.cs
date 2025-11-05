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

            builder.HasOne(m => m.User)
                   .WithMany(u => u.Motorcycles)
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
