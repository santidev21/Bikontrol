using Npgsql;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Guards the readiness connection string: it must keep the database host and
/// credentials while bounding the timeouts so <c>/ready</c> cannot hang.
/// </summary>
public sealed class HealthCheckConnectionStringTests
{
    [Fact]
    public void BuildHealthCheckConnectionString_ShouldBoundTimeoutsAndKeepHost()
    {
        const string original =
            "Host=bikontrol-db;Port=5432;Database=bikontrol_db;Username=bikontrol;Password=secret;SslMode=Require";

        var result = Program.BuildHealthCheckConnectionString(original);

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("bikontrol-db", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("bikontrol_db", builder.Database);
        Assert.Equal("bikontrol", builder.Username);
        Assert.Equal("secret", builder.Password);
        Assert.Equal(3, builder.Timeout);
        Assert.Equal(3, builder.CommandTimeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildHealthCheckConnectionString_WhenMissing_ShouldReturnItUnchanged(string? connectionString)
    {
        Assert.Equal(connectionString, Program.BuildHealthCheckConnectionString(connectionString));
    }
}
