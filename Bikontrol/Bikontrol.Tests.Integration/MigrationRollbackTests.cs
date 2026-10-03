using System.Data;
using Bikontrol.Persistence;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Applies, rolls back and re-applies the newest migration. This pins the
/// <c>Down()</c> method (never exercised by the app) and proves the migration is
/// reversible — the basis of a safe deploy/rollback (see the plan's DR item).
/// </summary>
[Collection("api")]
public sealed class MigrationRollbackTests
{
    private readonly PostgresApiFactory _factory;

    public MigrationRollbackTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task NewestMigration_ShouldRollBackAndReapply()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
        var migrations = db.GetInfrastructure().GetRequiredService<IMigrationsAssembly>();

        var ordered = migrations.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
        var newest = ordered[^1];
        var previous = ordered[^2];

        try
        {
            // Start from the previous migration so the rollback is a real step.
            await migrator.MigrateAsync(previous);
            Assert.True(await IsAppliedAsync(db, previous));
            Assert.False(await IsAppliedAsync(db, newest));

            // Re-apply the newest migration, then roll it back and re-apply it:
            // this exercises the generated Up() and Down() end to end, without
            // hard-coding the schema each migration happens to touch.
            await migrator.MigrateAsync();
            Assert.True(await IsAppliedAsync(db, newest));

            await migrator.MigrateAsync(previous);
            Assert.False(await IsAppliedAsync(db, newest));

            await migrator.MigrateAsync();
            Assert.True(await IsAppliedAsync(db, newest));
        }
        finally
        {
            // Always leave the shared database at the latest migration.
            await migrator.MigrateAsync();
        }
    }

    private static async Task<bool> IsAppliedAsync(AppDbContext db, string migrationId)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);

        var count = Convert.ToInt64(await command.ExecuteScalarAsync());
        return count > 0;
    }
}
