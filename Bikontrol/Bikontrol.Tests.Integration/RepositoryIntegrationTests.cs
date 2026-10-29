using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Exercises repository queries against real PostgreSQL: LINQ translation and
/// bulk deletes cannot be verified with the in-memory fakes of the unit tests.
/// </summary>
[Collection("api")]
public sealed class RepositoryIntegrationTests
{
    private readonly PostgresApiFactory _factory;

    public RepositoryIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task RefreshTokens_AddLookupRevokeAndPurge_ShouldRoundTrip()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var user = new User($"repo-{Guid.NewGuid():N}@bikontrol.test", "Repo", "hashed");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hash = $"hash-{Guid.NewGuid():N}";
        var expired = new RefreshToken(user.Id, $"old-{Guid.NewGuid():N}", DateTime.UtcNow.AddDays(-60));
        await tokens.AddAsync(new RefreshToken(user.Id, hash, DateTime.UtcNow.AddDays(10)));
        await tokens.AddAsync(expired);
        await tokens.SaveChangesAsync();

        var found = await tokens.GetByTokenHashAsync(hash);
        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.User!.Id);
        Assert.Null(await tokens.GetByTokenHashAsync("missing"));

        var revoked = await tokens.RevokeAllForUserAsync(user.Id);
        Assert.Equal(2, revoked);
        Assert.True(await tokens.TrySaveAsync());

        var purged = await tokens.DeleteInactiveBeforeAsync(DateTime.UtcNow.AddDays(-30));
        Assert.True(purged >= 1);
        Assert.Null(await tokens.GetByTokenHashAsync(expired.TokenHash));
        Assert.NotNull(await tokens.GetByTokenHashAsync(hash));
    }

    [RequiresDockerFact]
    public async Task KmHistory_Queries_ShouldReturnFirstLastAndBatchedValues()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var history = scope.ServiceProvider.GetRequiredService<IKmHistoryRepository>();
        var user = new User($"km-{Guid.NewGuid():N}@bikontrol.test", "Km", "hashed");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", user.Id);
        db.Motorcycles.Add(moto);
        await db.SaveChangesAsync();

        var first = DateTime.UtcNow.AddDays(-5);
        await history.AddAsync(new MotorcycleKmHistory { Id = Guid.NewGuid(), MotorcycleId = moto.Id, Km = 1000, RecordedAt = first });
        await history.AddAsync(new MotorcycleKmHistory { Id = Guid.NewGuid(), MotorcycleId = moto.Id, Km = 1500, RecordedAt = DateTime.UtcNow });
        await history.SaveChangesAsync();

        Assert.Equal(1500, (await history.GetLastByMotorcycleIdAsync(moto.Id))!.Km);
        Assert.Equal(1000, (await history.GetFirstByMotorcycleIdAsync(moto.Id))!.Km);

        var all = await history.GetByMotorcycleIdAsync(moto.Id);
        Assert.Equal(new[] { 1500, 1000 }, all.Select(x => x.Km));

        var latest = await history.GetLatestKmByMotorcycleIdsAsync(new[] { moto.Id, moto.Id });
        Assert.Equal(1500, latest[moto.Id]);
        Assert.Empty(await history.GetLatestKmByMotorcycleIdsAsync(Array.Empty<Guid>()));

        var initial = await history.GetInitialRecordedAtByMotorcycleIdsAsync(new[] { moto.Id });
        Assert.NotNull(initial[moto.Id]);
        Assert.Empty(await history.GetInitialRecordedAtByMotorcycleIdsAsync(Array.Empty<Guid>()));

        history.Remove((await history.GetLastByMotorcycleIdAsync(moto.Id))!);
        await history.SaveChangesAsync();
        Assert.Equal(1000, (await history.GetLastByMotorcycleIdAsync(moto.Id))!.Km);
    }
}
