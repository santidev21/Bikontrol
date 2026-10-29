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
            builder.Property(u => u.EmailConfirmedAt);
            builder.Property(u => u.EmailConfirmationTokenHash).HasMaxLength(128);
            builder.Property(u => u.EmailConfirmationTokenExpires);
            builder.Property(u => u.AccessFailedCount).IsRequired().HasDefaultValue(0);
            builder.Property(u => u.LockoutEnd);
            builder.Property(u => u.SecurityStamp).IsRequired();
            builder.Property(u => u.AuthProvider).HasMaxLength(30);
            builder.Property(u => u.RemindersEnabled).IsRequired().HasDefaultValue(true);
            builder.Property(u => u.MonthlyMaintenanceBudget).HasPrecision(12, 2);
            builder.Property(u => u.AnnualMaintenanceBudget).HasPrecision(12, 2);

            // Optimistic concurrency via xmin (Postgres system column).
            // Npgsql 9 removed UseXminAsConcurrencyToken(); mapping the shadow
            // property explicitly keeps xmin bound to the system column instead
            // of creating a physical "xmin" column.
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .HasColumnName("xmin")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }
    }
}
