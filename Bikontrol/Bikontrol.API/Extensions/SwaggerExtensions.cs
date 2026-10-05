using Microsoft.OpenApi.Models;

namespace Bikontrol.API.Extensions;

/// <summary>
/// Single source of truth for the OpenAPI/Swagger document. The API uses it at
/// runtime (Swagger UI in Development) and <c>ApiContractTests</c> reuses the
/// exact same configuration to generate the committed
/// <c>docs/api/openapi.json</c> snapshot, so the served document and the
/// contract test can never drift apart.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>Registers API explorer + Swagger generation with the JWT scheme.</summary>
    public static IServiceCollection AddBikontrolSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Bikontrol API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5..."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
