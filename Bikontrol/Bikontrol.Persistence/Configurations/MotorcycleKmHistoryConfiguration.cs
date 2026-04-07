using Bikontrol.Domain.Entities;
using Bikontrol.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Configurations
{
    public class MotorcycleKmHistoryConfiguration : IEntityTypeConfiguration<MotorcycleKmHistory>
    {
        public void Configure(EntityTypeBuilder<MotorcycleKmHistory> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Km)
                .IsRequired();

            builder.Property(x => x.RecordedAt)
                .IsRequired();

            builder.HasOne(x => x.Motorcycle)
                .WithMany(m => m.KmHistory)
                .HasForeignKey(x => x.MotorcycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.MotorcycleId, x.RecordedAt });
        }
    }
}
