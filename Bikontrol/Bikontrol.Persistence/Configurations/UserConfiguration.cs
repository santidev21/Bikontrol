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
    public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            builder.Property(u => u.CreatedAt).IsRequired();
        }
    }
}
