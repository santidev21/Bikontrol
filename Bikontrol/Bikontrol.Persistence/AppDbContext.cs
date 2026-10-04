using Bikontrol.Application.Interfaces;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bikontrol.Persistence
{
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUser;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
            : base(options)
        {
            _currentUser = currentUser;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
        public DbSet<Motorcycle> Motorcycles { get; set; } = default!;
        public DbSet<Maintenance> DefaultMaintenances { get; set; } = default!;
        public DbSet<UserMaintenance> UserMaintenances { get; set; } = default!;
        public DbSet<MotorcycleKmHistory> MotorcycleKmHistories { get; set; }
        public DbSet<MotorcycleMaintenanceRecord> MotorcycleMaintenanceRecords { get; set; } = default!;
        public DbSet<ReminderLog> ReminderLogs { get; set; } = default!;
        public DbSet<PushSubscription> PushSubscriptions { get; set; } = default!;
        public DbSet<MaintenanceRecordAttachment> MaintenanceRecordAttachments { get; set; } = default!;
        public DbSet<AuditLog> AuditLogs { get; set; } = default!;

        /// <summary>
        /// Entities whose changes are worth auditing. Infrastructure tables
        /// (tokens, push subscriptions, reminder logs) and the audit table itself
        /// are intentionally excluded to keep the trail meaningful and quiet.
        /// </summary>
        private static readonly HashSet<Type> AuditedTypes = new()
        {
            typeof(Motorcycle),
            typeof(UserMaintenance),
            typeof(MotorcycleMaintenanceRecord),
            typeof(MotorcycleKmHistory),
            typeof(MaintenanceRecordAttachment),
            typeof(User)
        };


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            WriteAuditLog();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void WriteAuditLog()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => AuditedTypes.Contains(e.Entity.GetType()))
                .ToList();
            if (entries.Count == 0)
                return;

            // Never log changes to the audit rows we are about to add.
            var userId = _currentUser is { UserId: var id } && id != Guid.Empty ? id : (Guid?)null;
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = ReadPrimaryKey(entry),
                    Action = entry.State switch
                    {
                        EntityState.Added => "Created",
                        EntityState.Deleted => "Deleted",
                        _ => "Updated"
                    },
                    Changes = BuildChanges(entry),
                    CreatedAt = now
                });
            }
        }

        private static string ReadPrimaryKey(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            var value = key?.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                .FirstOrDefault(v => v is not null);
            return value ?? string.Empty;
        }

        private static string? BuildChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();

            if (entry.State == EntityState.Modified)
            {
                foreach (var property in entry.Properties)
                {
                    if (!property.IsModified || property.OriginalValue?.Equals(property.CurrentValue) == true)
                        continue;

                    changes[property.Metadata.Name] = new
                    {
                        from = Sanitize(property.Metadata.Name, property.OriginalValue),
                        to = Sanitize(property.Metadata.Name, property.CurrentValue)
                    };
                }

                if (changes.Count == 0)
                    return null;
            }
            else
            {
                foreach (var property in entry.Properties)
                {
                    if (!property.Metadata.IsPrimaryKey() && property.CurrentValue is not null)
                        changes[property.Metadata.Name] = Sanitize(property.Metadata.Name, property.CurrentValue);
                }
            }

            return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
        }

        /// <summary>
        /// Stops secrets from ever reaching the trail: values of sensitive columns
        /// are replaced with a redaction marker.
        /// </summary>
        private static readonly HashSet<string> RedactedProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "RefreshTokenHash",
            "ResetPasswordTokenHash",
            "EmailConfirmationTokenHash"
        };

        private static object? Sanitize(string propertyName, object? value)
            => RedactedProperties.Contains(propertyName) ? "[redacted]" : value;
    }
}
