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

            // Concurrencia optimista con xmin (columna de sistema de Postgres):
            // dos escrituras simultáneas sobre la misma moto no se pisan en silencio.
            // Se usa el API específico de Npgsql porque IsRowVersion() estándar
            // crearía una columna física "xmin" que choca con la del sistema.
#pragma warning disable CS0618 // UseXminAsConcurrencyToken es obsoleto pero mapea la columna real del sistema
            builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

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
