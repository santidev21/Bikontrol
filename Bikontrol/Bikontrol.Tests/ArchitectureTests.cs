using System.Reflection;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence;

namespace Bikontrol.Tests;

/// <summary>
/// Pins the layer boundaries (Domain ← Application ← Infrastructure/Persistence ← API)
/// so business logic cannot silently grow a dependency on a framework or SDK.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(User).Assembly;
    private static readonly Assembly Application = typeof(Bikontrol.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Bikontrol.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Persistence = typeof(AppDbContext).Assembly;

    private static IEnumerable<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    [Theory]
    [InlineData("Bikontrol.Infrastructure")]
    [InlineData("Bikontrol.Persistence")]
    [InlineData("Bikontrol.API")]
    public void Domain_ShouldNotDependOnOuterLayers(string forbidden) =>
        Assert.DoesNotContain(forbidden, References(Domain));

    [Theory]
    [InlineData("Bikontrol.Infrastructure")]
    [InlineData("Bikontrol.Persistence")]
    [InlineData("Bikontrol.API")]
    public void Application_ShouldNotDependOnOuterLayers(string forbidden) =>
        Assert.DoesNotContain(forbidden, References(Application));

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    [InlineData("QuestPDF")]
    [InlineData("Google.Apis.Auth")]
    [InlineData("MailKit")]
    [InlineData("WebPush")]
    [InlineData("Microsoft.AspNetCore.Http.Abstractions")]
    public void Application_ShouldNotDependOnFrameworksOrSdks(string forbidden) =>
        Assert.DoesNotContain(forbidden, References(Application));

    [Fact]
    public void Infrastructure_ShouldNotDependOnPersistence() =>
        Assert.DoesNotContain("Bikontrol.Persistence", References(Infrastructure));

    [Fact]
    public void Persistence_ShouldNotDependOnInfrastructure() =>
        Assert.DoesNotContain("Bikontrol.Infrastructure", References(Persistence));
}
