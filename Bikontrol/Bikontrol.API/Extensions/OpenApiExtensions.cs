using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bikontrol.API.Extensions;

/// <summary>
/// Single source of truth for the OpenAPI document. The API uses it at runtime
/// (document served at <c>/openapi/v1.json</c>, Scalar UI in Development) and
/// <c>ApiContractTests</c> reuses the same registration through
/// <see cref="Microsoft.AspNetCore.OpenApi.IOpenApiDocumentProvider"/> to
/// generate the committed <c>docs/api/openapi.json</c> snapshot, so the served
/// document and the contract test can never drift apart.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Registers the native OpenAPI generator for the <c>v1</c> document and adds
    /// the Bearer (JWT) security scheme, mirroring the previous Swagger setup.
    /// </summary>
    public static IServiceCollection AddBikontrolOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Bikontrol API";
                document.Info.Version = "v1";

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your valid JWT token."
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
