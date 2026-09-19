using Docker.DotNet;

namespace Bikontrol.Tests.Integration.Infrastructure;

/// <summary>
/// Detects whether a Docker daemon is reachable, so integration tests can skip
/// gracefully on machines without Docker instead of failing the whole run.
/// </summary>
public static class DockerProbe
{
    private static readonly Lazy<bool> Available = new(() =>
    {
        try
        {
            using var client = new DockerClientConfiguration().CreateClient();
            client.System.PingAsync().GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    });

    public static bool IsAvailable => Available.Value;
}

/// <summary>A <see cref="FactAttribute"/> that skips when Docker is unavailable.</summary>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerProbe.IsAvailable)
        {
            Skip = "Docker is not available; skipping integration test.";
        }
    }
}
