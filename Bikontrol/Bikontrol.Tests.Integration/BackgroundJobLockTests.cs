using Bikontrol.Application.Interfaces;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// The advisory lock keeps a scaled-out API from running a background job twice.
/// </summary>
[Collection("api")]
public sealed class BackgroundJobLockTests
{
    private readonly PostgresApiFactory _factory;

    public BackgroundJobLockTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task TryAcquire_WhileAnotherHolderHasTheLock_ShouldReturnNullUntilReleased()
    {
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();
        var lockA = first.ServiceProvider.GetRequiredService<IDistributedLock>();
        var lockB = second.ServiceProvider.GetRequiredService<IDistributedLock>();
        var name = $"test:lock:{Guid.NewGuid()}";

        var handle = await lockA.TryAcquireAsync(name);
        Assert.NotNull(handle);

        Assert.Null(await lockB.TryAcquireAsync(name));

        await handle!.DisposeAsync();

        var reacquired = await lockB.TryAcquireAsync(name);
        Assert.NotNull(reacquired);
        await reacquired!.DisposeAsync();
    }

    [RequiresDockerFact]
    public async Task TryAcquire_DifferentNames_ShouldNotBlockEachOther()
    {
        using var scope = _factory.Services.CreateScope();
        var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();

        var a = await distributedLock.TryAcquireAsync($"test:a:{Guid.NewGuid()}");
        var b = await distributedLock.TryAcquireAsync($"test:b:{Guid.NewGuid()}");

        Assert.NotNull(a);
        Assert.NotNull(b);
        await a!.DisposeAsync();
        await b!.DisposeAsync();
    }
}
