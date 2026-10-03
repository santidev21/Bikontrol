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
        var previous = ordered[^2];

        try
        {
            await migrator.MigrateAsync(previous);
            Assert.False(await ColumnExistsAsync(db, "EmailConfirmedAt"));

            await migrator.MigrateAsync();
            Assert.True(await ColumnExistsAsync(db, "EmailConfirmedAt"));
        }
        finally
        {
            // Always leave the shared database at the latest migration.
            await migrator.MigrateAsync();
        }
    }

    private static async Task<bool> ColumnExistsAsync(AppDbContext db, string column)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'users' AND column_name = @column";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@column";
        parameter.Value = column;
        command.Parameters.Add(parameter);

        var count = Convert.ToInt64(await command.ExecuteScalarAsync());
        return count > 0;
    }
}
