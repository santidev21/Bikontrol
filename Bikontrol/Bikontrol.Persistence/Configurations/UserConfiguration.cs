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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            builder.Property(u => u.Role).IsRequired().HasMaxLength(20).HasDefaultValue(UserRole.User);
            builder.Property(u => u.CreatedAt).IsRequired();
            builder.Property(u => u.ResetPasswordTokenHash).HasMaxLength(128);
            builder.Property(u => u.ResetPasswordTokenExpires);
            builder.Property(u => u.AuthProvider).HasMaxLength(30);

            // Concurrencia optimista con xmin (columna de sistema de Postgres).
            // Se usa el API específico de Npgsql porque IsRowVersion() estándar
            // crearía una columna física "xmin" que choca con la del sistema.
#pragma warning disable CS0618 // UseXminAsConcurrencyToken es obsoleto pero mapea la columna real del sistema
            builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618
        }
    }
}
