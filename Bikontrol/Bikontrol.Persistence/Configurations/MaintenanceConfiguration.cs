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
    public class MaintenanceConfiguration : IEntityTypeConfiguration<Maintenance>
    {
        public void Configure(EntityTypeBuilder<Maintenance> builder)
        {
            builder.ToTable("MaintenanceTypes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Description)
                .HasColumnType("text");

            builder.Property(x => x.DefaultKmInterval);

            builder.Property(x => x.DefaultTimeIntervalWeeks);

            builder.Property(x => x.TrackingType)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("Km");

            builder.Property(x => x.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder
                .HasMany(x => x.UserMaintenanceTypes)
                .WithOne(x => x.BaseType)
                .HasForeignKey(x => x.BaseTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasData(
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "Cambio de Aceite",
                    Description = "Reemplazo del aceite del motor.",
                    DefaultKmInterval = 1500,
                    DefaultTimeIntervalWeeks = 6,
                    TrackingType = "Km",
                    IsEnabled = true
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Name = "Cambio de Filtro de Aceite",
                    Description = "Sustitución del filtro de aceite del motor.",
                    DefaultKmInterval = 1500,
                    DefaultTimeIntervalWeeks = 6,
                    TrackingType = "Km",
                    IsEnabled = true
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Name = "Lubricación y Limpieza de Cadena",
                    Description = "Limpieza y lubricación de la cadena de transmisión.",
                    DefaultKmInterval = 500,
                    DefaultTimeIntervalWeeks = 2,
                    TrackingType = "Time",
                    IsEnabled = true
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                    Name = "Revisión General",
                    Description = "Chequeo completo del estado general de la motocicleta.",
                    DefaultKmInterval = 20000,
                    DefaultTimeIntervalWeeks = 80,
                    TrackingType = "Km",
                    IsEnabled = true
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    Name = "Mantenimiento Preventivo",
                    Description = "Revisión preventiva del estado general de la motocicleta.",
                    DefaultKmInterval = 5000,
                    DefaultTimeIntervalWeeks = 20,
                    TrackingType = "Km",
                    IsEnabled = true
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                    Name = "Filtro de Aire",
                    Description = "Limpieza o reemplazo del filtro de aire.",
                    DefaultKmInterval = 6000,
                    DefaultTimeIntervalWeeks = 24,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                    Name = "Filtro de Gasolina",
                    Description = "Reemplazo o limpieza del filtro de gasolina.",
                    DefaultKmInterval = 8000,
                    DefaultTimeIntervalWeeks = 32,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                    Name = "Presión de Llantas",
                    Description = "Verificación y ajuste de la presión de las llantas.",
                    DefaultKmInterval = 500,
                    DefaultTimeIntervalWeeks = 1,
                    TrackingType = "Time"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
                    Name = "Pastillas de Freno Delanteras",
                    Description = "Revisión y reemplazo de las pastillas de freno delanteras.",
                    DefaultKmInterval = 8000,
                    DefaultTimeIntervalWeeks = 32,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000010"),
                    Name = "Pastillas de Freno Traseras",
                    Description = "Revisión y reemplazo de las pastillas de freno traseras.",
                    DefaultKmInterval = 8000,
                    DefaultTimeIntervalWeeks = 32,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000011"),
                    Name = "Disco de Freno Delantero",
                    Description = "Revisión del disco de freno delantero.",
                    DefaultKmInterval = 25000,
                    DefaultTimeIntervalWeeks = 100,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000012"),
                    Name = "Disco de Freno Trasero",
                    Description = "Revisión del disco de freno trasero.",
                    DefaultKmInterval = 25000,
                    DefaultTimeIntervalWeeks = 100,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000013"),
                    Name = "Líquido de Frenos Delantero",
                    Description = "Reemplazo del líquido de frenos delantero.",
                    DefaultKmInterval = 13000,
                    DefaultTimeIntervalWeeks = 52,
                    TrackingType = "Time"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000014"),
                    Name = "Líquido de Frenos Trasero",
                    Description = "Reemplazo del líquido de frenos trasero.",
                    DefaultKmInterval = 13000,
                    DefaultTimeIntervalWeeks = 52,
                    TrackingType = "Time"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000015"),
                    Name = "Batería",
                    Description = "Revisión del estado y carga de la batería.",
                    DefaultKmInterval = 10000,
                    DefaultTimeIntervalWeeks = 52,
                    TrackingType = "Time"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000016"),
                    Name = "Suspensión",
                    Description = "Revisión de suspensión delantera y trasera.",
                    DefaultKmInterval = 10000,
                    DefaultTimeIntervalWeeks = 40,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000017"),
                    Name = "Tornillería",
                    Description = "Ajuste general de tornillería.",
                    DefaultKmInterval = 2000,
                    DefaultTimeIntervalWeeks = 8,
                    TrackingType = "Time"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000018"),
                    Name = "Calibración de Válvulas",
                    Description = "Ajuste de holguras de válvulas.",
                    DefaultKmInterval = 8000,
                    DefaultTimeIntervalWeeks = 32,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000019"),
                    Name = "Kit de Arrastre - Piñón",
                    Description = "Revisión o reemplazo del piñón.",
                    DefaultKmInterval = 12000,
                    DefaultTimeIntervalWeeks = 48,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000020"),
                    Name = "Kit de Arrastre - Corona",
                    Description = "Revisión o reemplazo de la corona.",
                    DefaultKmInterval = 12000,
                    DefaultTimeIntervalWeeks = 48,
                    TrackingType = "Km"
                },
                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000021"),
                    Name = "Kit de Arrastre - Cadena",
                    Description = "Revisión o reemplazo de la cadena.",
                    DefaultKmInterval = 12000,
                    DefaultTimeIntervalWeeks = 48,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000022"),
                    Name = "Tensión de Cadena",
                    Description = "Ajuste de la tensión de la cadena.",
                    DefaultKmInterval = 500,
                    DefaultTimeIntervalWeeks = 2,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000023"),
                    Name = "Sincronización",
                    Description = "Ajuste de mezcla/ralentí o sistema de inyección.",
                    DefaultKmInterval = 8000,
                    DefaultTimeIntervalWeeks = 32,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000024"),
                    Name = "Bandas de Freno",
                    Description = "Reemplazo de bandas de freno (frenos de tambor).",
                    DefaultKmInterval = 8000,
                    DefaultTimeIntervalWeeks = 32,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000025"),
                    Name = "Bujía",
                    Description = "Revisión o reemplazo de bujía.",
                    DefaultKmInterval = 4000,
                    DefaultTimeIntervalWeeks = 16,
                    TrackingType = "Km"
                },

                new Maintenance
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000026"),
                    Name = "Neumáticos",
                    Description = "Revisión del desgaste de los neumáticos.",
                    DefaultKmInterval = 15000,
                    DefaultTimeIntervalWeeks = 60,
                    TrackingType = "Km"
                }
            );
        }
    }
}
