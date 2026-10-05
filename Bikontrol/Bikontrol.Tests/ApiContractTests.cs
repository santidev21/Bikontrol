using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bikontrol.API.Controllers;
using Bikontrol.API.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace Bikontrol.Tests;

/// <summary>
/// Guards the HTTP contract between the API and its clients (Angular). It
/// produces two committed snapshots and fails the build when they change without
/// being refreshed, forcing a conscious decision about whether the client must
/// change in the same pass:
///
/// <list type="bullet">
/// <item><c>docs/api/openapi.json</c> — the OpenAPI document: routes, parameters,
/// request bodies and validation. Built with the same Swagger + JSON
/// configuration the app serves (via
/// <see cref="SwaggerExtensions.AddBikontrolSwagger"/>).</item>
/// <item><c>docs/api/dto-contract.json</c> — the JSON shape (property names,
/// types, required/optional) of every DTO. Controllers return
/// <c>IActionResult</c>, so Swashbuckle cannot infer response types; this
/// reflection snapshot covers responses too and uses the verbatim JSON property
/// names, which the camelCased Swagger document cannot distinguish.</item>
/// </list>
///
/// Refresh both with <c>npm run api:contract:update</c> and review the diff.
/// </summary>
public class ApiContractTests
{
    private const string UpdateEnvironmentVariable = "UPDATE_API_CONTRACT";

    [Fact]
    public void OpenApiDocument_ShouldMatchCommittedSnapshot() =>
        AssertSnapshot("openapi.json", GenerateCanonicalDocument());

    [Fact]
    public void DtoShapes_ShouldMatchCommittedSnapshot() =>
        AssertSnapshot("dto-contract.json", GenerateCanonicalDtoContract());

    /// <summary>
    /// Compares <paramref name="actual"/> against the committed snapshot,
    /// rewriting it in place when <c>UPDATE_API_CONTRACT=1</c>.
    /// </summary>
    private static void AssertSnapshot(string fileName, string actual)
    {
        var snapshotPath = Path.Combine(ContractDirectory(), fileName);

        if (Environment.GetEnvironmentVariable(UpdateEnvironmentVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            File.WriteAllText(snapshotPath, actual + Environment.NewLine);
            return;
        }

        Assert.True(
            File.Exists(snapshotPath),
            $"Contract snapshot not found at {snapshotPath}. Run `npm run api:contract:update`.");

        var expected = File.ReadAllText(snapshotPath)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd();

        Assert.True(
            string.Equals(expected, actual, StringComparison.Ordinal),
            $"The API contract changed but the committed snapshot '{fileName}' did not.\n" +
            "If the change is visible to the API clients, update the Angular types/services in the " +
            $"same pass, then run `npm run api:contract:update` to refresh {snapshotPath}.");
    }

    /// <summary>
    /// Builds the OpenAPI v1 document and serializes it as canonical JSON so the
    /// snapshot is stable regardless of route/schema discovery order. The
    /// service collection mirrors the API's MVC and JSON pipeline so the
    /// generated shape matches what the app actually serves; the Swagger
    /// registration itself is shared with the running app.
    /// </summary>
    private static string GenerateCanonicalDocument()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Swashbuckle's API explorer resolves a host environment and the entry
        // assembly for application parts; minimal stubs are enough (no requests
        // are served, no files are read) as long as ApplicationName points at a
        // loadable assembly.
        var environment = new StubHostEnvironment(
            typeof(ApiContractTests).Assembly.GetName().Name!);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<IWebHostEnvironment>(environment);

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Mirrors ASP.NET Core's default System.Text.Json setup, which the
                // real app relies on (no custom AddJsonOptions in Program.cs).
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            })
            .AddApplicationPart(typeof(AuthController).Assembly);

        services.AddBikontrolSwagger();

        using var provider = services.BuildServiceProvider();
        var document = provider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        using var writer = new StringWriter();
        document.SerializeAsV3(new OpenApiJsonWriter(writer));

        return Canonicalize(writer.ToString());
    }

    /// <summary>
    /// Reflects every DTO and records its JSON member names and value types, so a
    /// renamed/added/removed property or a type change is caught even when
    /// Swashbuckle does not expose the DTO (responses, nested objects).
    /// </summary>
    private static string GenerateCanonicalDtoContract()
    {
        var dtoTypes = typeof(AuthController).Assembly.GetTypes()
            .Concat(typeof(Bikontrol.Application.DTOs.Motorcycle.MotorcycleDTO).Assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsNested && t.Namespace?.Contains(".DTOs") == true)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var contract = new SortedDictionary<string, object?>(StringComparer.Ordinal);

        foreach (var type in dtoTypes)
        {
            var properties = new SortedDictionary<string, string?>(StringComparer.Ordinal);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetMethod is null)
                {
                    continue;
                }

                var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                var jsonName = attribute?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                properties[jsonName] = Describe(property.PropertyType);
            }

            contract[type.FullName!] = properties;
        }

        return JsonSerializer.Serialize(contract, ReadableJsonOptions);
    }

    /// <summary>A stable, assembly-agnostic description of a member's JSON type.</summary>
    private static string Describe(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var isNullable = underlying is not null;
        var effective = underlying ?? type;

        if (effective == typeof(string))
        {
            return isNullable ? "string?" : "string";
        }

        if (effective == typeof(Guid))
        {
            return isNullable ? "guid?" : "guid";
        }

        if (effective == typeof(DateTime) || effective == typeof(DateTimeOffset))
        {
            return isNullable ? "datetime?" : "datetime";
        }

        if (effective == typeof(bool))
        {
            return isNullable ? "bool?" : "bool";
        }

        if (effective == typeof(int) || effective == typeof(long) || effective == typeof(short))
        {
            return isNullable ? "integer?" : "integer";
        }

        if (effective == typeof(decimal) || effective == typeof(double) || effective == typeof(float))
        {
            return isNullable ? "number?" : "number";
        }

        if (effective.IsGenericType)
        {
            var name = effective.Name.Split('`')[0];
            var arguments = string.Join(",", effective.GetGenericArguments().Select(Describe));
            return $"{name}<{arguments}>";
        }

        if (effective.IsArray)
        {
            return $"array<{Describe(effective.GetElementType()!)}>";
        }

        return effective.Name;
    }

    /// <summary>
    /// Parses JSON and re-serializes it with a stable, recursive key order so the
    /// snapshot does not depend on discovery/reflection order.
    /// </summary>
    private static string Canonicalize(string json)
    {
        var value = JsonSerializer.Deserialize<object>(json);
        return JsonSerializer.Serialize(Sort(value), ReadableJsonOptions);
    }

    private static readonly JsonSerializerOptions ReadableJsonOptions = new()
    {
        WriteIndented = true,
    };

    private static object? Sort(object? value)
    {
        switch (value)
        {
            case IDictionary<string, object?> map:
                var sorted = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var key in map.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    sorted[key] = Sort(map[key]);
                }
                return sorted;

            case IEnumerable<object?> list when value is not string:
                var items = new List<object?>();
                foreach (var item in list)
                {
                    items.Add(Sort(item));
                }
                return items;

            default:
                return value;
        }
    }

    /// <summary>Walks up from the test output directory to <c>docs/api</c>.</summary>
    private static string ContractDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Bikontrol.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repository root (Bikontrol.sln).");
        }

        var repositoryRoot = Directory.GetParent(directory.FullName)!.FullName;
        return Path.Combine(repositoryRoot, "docs", "api");
    }

    /// <summary>Minimal host environment so Swashbuckle can read API metadata.</summary>
    private sealed class StubHostEnvironment : IWebHostEnvironment
    {
        public StubHostEnvironment(string applicationName) => ApplicationName = applicationName;

        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
