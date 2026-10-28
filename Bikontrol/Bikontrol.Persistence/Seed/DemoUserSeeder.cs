using Bikontrol.Domain.Entities;
using Bikontrol.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Bikontrol.Persistence.Seed
{
    public static class DemoUserSeeder
    {
        private static async Task SeedDemoContentAsync(AppDbContext context, User demoUser, ILogger logger)
        {
            var hasMotorcycles = await context.Motorcycles.AnyAsync(m => m.UserId == demoUser.Id);
            if (hasMotorcycles)
            {
                logger.LogInformation("Demo content already exists for {Email}", demoUser.Email);
                return;
            }

            // Ensure default maintenance types exist before referencing them
            var defaults = await context.DefaultMaintenances.Where(d => d.IsEnabled).ToListAsync();
            if (defaults.Count == 0)
            {
                logger.LogWarning("No default maintenance types found — skipping demo maintenances");
                defaults = new List<Domain.Entities.Maintenance>();
            }

            Maintenance PickDefault(Guid id) => defaults.FirstOrDefault(d => d.Id == id) ?? defaults.FirstOrDefault()!;

            var moto1 = new Domain.Entities.Motorcycle("YBR 125", "Yamaha", 2022, "La Negra", 125, "DEM-001", demoUser.Id)
            {
                Image = "default.png"
            };
            var moto2 = new Domain.Entities.Motorcycle("CB125F", "Honda", 2023, "La Roja", 124, "DEM-002", demoUser.Id)
            {
                Image = "default.png"
            };

            await context.Motorcycles.AddRangeAsync(new[] { moto1, moto2 });
            await context.SaveChangesAsync();

            // Km history: moto1 -> 5.000 (30 days ago), 8.500 (now); moto2 -> 12.000 (45 days ago), 14.200 (now)
            var now = DateTime.UtcNow;
            var histories = new List<Domain.Entities.MotorcycleKmHistory>
            {
                new() { MotorcycleId = moto1.Id, Km = 5000, RecordedAt = now.AddDays(-30) },
                new() { MotorcycleId = moto1.Id, Km = 8500, RecordedAt = now.AddDays(-2) },
                new() { MotorcycleId = moto2.Id, Km = 12000, RecordedAt = now.AddDays(-45) },
                new() { MotorcycleId = moto2.Id, Km = 14200, RecordedAt = now.AddDays(-1) },
            };
            await context.MotorcycleKmHistories.AddRangeAsync(histories);
            await context.SaveChangesAsync();

            // Fuel-ups: a full-to-full history so the demo consumption and cost
            // stats look real (including one partial fill, which the calculator
            // rolls into the next full tank).
            var fuelUps = new List<Domain.Entities.FuelUp>
            {
                new() { MotorcycleId = moto1.Id, FueledAt = now.AddDays(-30), OdometerKm = 7000, Litres = 4.0m, TotalCost = 8.00m },
                new() { MotorcycleId = moto1.Id, FueledAt = now.AddDays(-22), OdometerKm = 7400, Litres = 4.1m, TotalCost = 8.20m },
                new() { MotorcycleId = moto1.Id, FueledAt = now.AddDays(-14), OdometerKm = 7800, Litres = 2.0m, TotalCost = 4.00m, IsFullTank = false },
                new() { MotorcycleId = moto1.Id, FueledAt = now.AddDays(-12), OdometerKm = 8000, Litres = 2.2m, TotalCost = 4.40m },
                new() { MotorcycleId = moto1.Id, FueledAt = now.AddDays(-2), OdometerKm = 8500, Litres = 4.2m, TotalCost = 8.40m },
                new() { MotorcycleId = moto2.Id, FueledAt = now.AddDays(-40), OdometerKm = 12000, Litres = 5.0m, TotalCost = 10.00m },
                new() { MotorcycleId = moto2.Id, FueledAt = now.AddDays(-30), OdometerKm = 12500, Litres = 5.2m, TotalCost = 10.40m },
                new() { MotorcycleId = moto2.Id, FueledAt = now.AddDays(-20), OdometerKm = 13000, Litres = 5.1m, TotalCost = 10.20m },
                new() { MotorcycleId = moto2.Id, FueledAt = now.AddDays(-10), OdometerKm = 13500, Litres = 5.3m, TotalCost = 10.60m },
                new() { MotorcycleId = moto2.Id, FueledAt = now.AddDays(-1), OdometerKm = 14200, Litres = 5.0m, TotalCost = 10.00m },
            };
            await context.FuelUps.AddRangeAsync(fuelUps);
            await context.SaveChangesAsync();

            if (defaults.Count > 0)
            {
                // User maintenances for demo: follow a few defaults per moto
                var aceiteBase = PickDefault(Guid.Parse("10000000-0000-0000-0000-000000000001"));
                var cadenaBase = PickDefault(Guid.Parse("10000000-0000-0000-0000-000000000003"));
                var bujiaBase = PickDefault(Guid.Parse("10000000-0000-0000-0000-000000000025"));
                var pastillasBase = PickDefault(Guid.Parse("10000000-0000-0000-0000-000000000009"));

                var um1 = new Domain.Entities.UserMaintenance
                {
                    UserId = demoUser.Id,
                    MotorcycleId = moto1.Id,
                    BaseTypeId = aceiteBase.Id,
                    Name = aceiteBase.Name,
                    Description = aceiteBase.Description,
                    KmInterval = aceiteBase.DefaultKmInterval,
                    TimeIntervalWeeks = aceiteBase.DefaultTimeIntervalWeeks,
                    TrackingType = aceiteBase.TrackingType,
                    IsEnabled = true
                };
                var um2 = new Domain.Entities.UserMaintenance
                {
                    UserId = demoUser.Id,
                    MotorcycleId = moto1.Id,
                    BaseTypeId = cadenaBase.Id,
                    Name = cadenaBase.Name,
                    Description = cadenaBase.Description,
                    KmInterval = cadenaBase.DefaultKmInterval,
                    TimeIntervalWeeks = cadenaBase.DefaultTimeIntervalWeeks,
                    TrackingType = cadenaBase.TrackingType,
                    IsEnabled = true
                };
                var um3 = new Domain.Entities.UserMaintenance
                {
                    UserId = demoUser.Id,
                    MotorcycleId = moto2.Id,
                    BaseTypeId = aceiteBase.Id,
                    Name = aceiteBase.Name,
                    Description = aceiteBase.Description,
                    KmInterval = aceiteBase.DefaultKmInterval,
                    TimeIntervalWeeks = aceiteBase.DefaultTimeIntervalWeeks,
                    TrackingType = aceiteBase.TrackingType,
                    IsEnabled = true
                };
                var um4 = new Domain.Entities.UserMaintenance
                {
                    UserId = demoUser.Id,
                    MotorcycleId = moto2.Id,
                    BaseTypeId = bujiaBase.Id,
                    Name = bujiaBase.Name,
                    Description = bujiaBase.Description,
                    KmInterval = bujiaBase.DefaultKmInterval,
                    TimeIntervalWeeks = bujiaBase.DefaultTimeIntervalWeeks,
                    TrackingType = bujiaBase.TrackingType,
                    IsEnabled = true
                };
                var um5 = new Domain.Entities.UserMaintenance
                {
                    UserId = demoUser.Id,
                    MotorcycleId = moto1.Id,
                    BaseTypeId = pastillasBase.Id,
                    Name = pastillasBase.Name,
                    Description = pastillasBase.Description,
                    KmInterval = pastillasBase.DefaultKmInterval,
                    TimeIntervalWeeks = pastillasBase.DefaultTimeIntervalWeeks,
                    TrackingType = pastillasBase.TrackingType,
                    IsEnabled = true
                };

                await context.UserMaintenances.AddRangeAsync(new[] { um1, um2, um3, um4, um5 });
                await context.SaveChangesAsync();

                // Maintenance records: show some history
                var records = new List<Domain.Entities.MotorcycleMaintenanceRecord>
                {
                    new() { MotorcycleId = moto1.Id, UserMaintenanceId = um1.Id, PerformedAt = now.AddDays(-14), PerformedKm = 6500 },
                    new() { MotorcycleId = moto1.Id, UserMaintenanceId = um2.Id, PerformedAt = now.AddDays(-7), PerformedKm = null },
                    new() { MotorcycleId = moto2.Id, UserMaintenanceId = um3.Id, PerformedAt = now.AddDays(-20), PerformedKm = 12800 },
                };
                await context.MotorcycleMaintenanceRecords.AddRangeAsync(records);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Demo content seeded: 2 motos, km history, maintenances for {Email}", demoUser.Email);
        }

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoUserSeeder");
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            // Ensure DB is migrated — for dev the DB is already migrated via npm run db:migrate;
            // in prod Program.cs already called Migrate. This call is idempotent, but only auto-migrate
            // in environments where we already decided to migrate (non-dev). To avoid double-migrate in dev,
            // we just ensure the users table query doesn't throw.
            try
            {
                var demoEmail = config["DemoUser:Email"] ?? "demo@bikontrol.com";
                var demoName = config["DemoUser:FullName"] ?? "Usuario Demo";

                var demoUser = await context.Users.FirstOrDefaultAsync(u => u.Email == demoEmail);
                if (demoUser == null)
                {
                    var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                    demoUser = new User(demoEmail, demoName, passwordHasher.HashPassword(null!, randomPassword), UserRole.Demo);
                    await context.Users.AddAsync(demoUser);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Demo user seeded: {Email}", demoEmail);
                }
                else if (demoUser.Role != UserRole.Demo)
                {
                    // The configured demo email belongs to a real account.
                    // Never seed demo content into it and never convert it.
                    logger.LogWarning("Demo seeding skipped: {Email} already exists with role {Role}", demoEmail, demoUser.Role);
                    return;
                }
                else if (demoUser.IsLockedOut)
                {
                    // Bring the demo account back online for visitors (a locked
                    // demo account blocks the login button until the lockout expires).
                    demoUser.ResetAccessFailed();
                    await context.SaveChangesAsync();
                    logger.LogInformation("Demo user lockout cleared: {Email}", demoEmail);
                }
                else
                {
                    logger.LogInformation("Demo user already exists: {Email}", demoEmail);
                }

                await SeedDemoContentAsync(context, demoUser, logger);
            }
            catch (Exception ex)
            {
                // Demo seeding is best-effort; don't crash startup if DB isn't ready yet (e.g. dev before migrate)
                logger.LogWarning(ex, "Demo user seeding skipped: {Message}", ex.Message);
            }
        }
    }
}
