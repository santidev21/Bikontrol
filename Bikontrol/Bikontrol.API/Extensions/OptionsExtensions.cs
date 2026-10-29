using Microsoft.Extensions.Options;

namespace Bikontrol.API.Extensions;

public static class OptionsExtensions
{
    /// <summary>
    /// Binds <typeparamref name="T"/> to a configuration section, validates its
    /// data annotations (and <c>IValidatableObject</c>) and fails the host start
    /// when the values are invalid.
    /// </summary>
    public static IServiceCollection AddValidatedOptions<T>(this IServiceCollection services, string section)
        where T : class
    {
        services.AddOptions<T>()
            .BindConfiguration(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
