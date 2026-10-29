using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Bikontrol.Tests;

internal static class TestOptions
{
    /// <summary>Binds a configuration section to its typed options, like the host does.</summary>
    public static IOptions<T> From<T>(IConfiguration configuration, string section) where T : class, new() =>
        Options.Create(configuration.GetSection(section).Get<T>() ?? new T());
}
