using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikontrol.Persistence.Configurations
{
    public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
    {
        public void Configure(EntityTypeBuilder<PushSubscription> builder)
        {
            builder.ToTable("push_subscriptions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Endpoint).IsRequired().HasMaxLength(500);
            builder.Property(s => s.P256dh).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Auth).IsRequired().HasMaxLength(100);
            builder.Property(s => s.CreatedAt).IsRequired();

            // One subscription per endpoint; a re-subscribe upserts it.
            builder.HasIndex(s => s.Endpoint).IsUnique();
            builder.HasIndex(s => s.UserId);

            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
