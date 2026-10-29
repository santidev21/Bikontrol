using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bikontrol.Tests.Integration.Infrastructure;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Authorization boundary over the real API + PostgreSQL: demo accounts are
/// read-only and users cannot touch each other's motorcycles.
/// </summary>
[Collection("api")]
public sealed class AuthorizationIntegrationTests
{
    private readonly PostgresApiFactory _factory;

    public AuthorizationIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task DemoUser_WriteMotorcycle_ShouldReturn403AndPersistNothing()
    {
        var client = _factory.CreateClient();
        var demo = await client.PostAsJsonAsync("/api/auth/demo", new { });
        demo.EnsureSuccessStatusCode();
        var auth = await demo.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var before = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");

        var create = await client.PostAsJsonAsync("/api/motorcycles", new
        {
            name = "XTZ 150",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "Demo write",
            km = 100,
            displacement = 150,
            plate = "DEM999",
            image = "default.png"
        });

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var after = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.Equal(before!.Count, after!.Count);
    }

    [RequiresDockerFact]
    public async Task User_CannotReadOrDeleteOtherUsersMotorcycle()
    {
        var ownerClient = await RegisterClientAsync();
        var created = await CreateMotorcycleAsync(ownerClient);

        var otherClient = await RegisterClientAsync();

        var get = await otherClient.GetAsync($"/api/motorcycles/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);

        var delete = await otherClient.DeleteAsync($"/api/motorcycles/{created.Id}");
        Assert.True(delete.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound, $"Unexpected {delete.StatusCode}");

        // The owner's motorcycle is untouched.
        var mine = await ownerClient.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.Contains(mine!, m => m.Id == created.Id);
    }

    [RequiresDockerFact]
    public async Task User_CannotReachAnyChildResourceOfAnotherUsersMotorcycle()
    {
        var owner = await RegisterClientAsync();
        var moto = await CreateMotorcycleAsync(owner);

        var fuel = await owner.PostAsJsonAsync("/api/fuel-ups", new
        {
            motorcycleId = moto.Id,
            fueledAt = DateTime.UtcNow,
            odometerKm = 1100,
            litres = 5m,
            totalCost = 20m,
            isFullTank = true
        });
        fuel.EnsureSuccessStatusCode();
        var fuelUp = await fuel.Content.ReadFromJsonAsync<IdResponse>();

        var maintenance = await owner.PostAsJsonAsync("/api/maintenances/mine", new
        {
            motorcycleId = moto.Id,
            name = "Cambio de aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        });
        maintenance.EnsureSuccessStatusCode();
        var userMaintenance = await maintenance.Content.ReadFromJsonAsync<IdResponse>();

        var record = await owner.PostAsJsonAsync("/api/maintenances/records", new
        {
            motorcycleId = moto.Id,
            userMaintenanceId = userMaintenance!.Id,
            performedAt = DateTime.UtcNow,
            performedKm = 1050
        });
        record.EnsureSuccessStatusCode();
        var recordId = (await record.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var intruder = await RegisterClientAsync();
        var denied = new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound };

        var attempts = new Dictionary<string, HttpResponseMessage>
        {
            ["fuel list"] = await intruder.GetAsync($"/api/fuel-ups/motorcycle/{moto.Id}"),
            ["fuel consumption"] = await intruder.GetAsync($"/api/fuel-ups/motorcycle/{moto.Id}/consumption"),
            ["fuel get"] = await intruder.GetAsync($"/api/fuel-ups/{fuelUp!.Id}"),
            ["fuel delete"] = await intruder.DeleteAsync($"/api/fuel-ups/{fuelUp.Id}"),
            ["fuel create"] = await intruder.PostAsJsonAsync("/api/fuel-ups", new
            {
                motorcycleId = moto.Id,
                fueledAt = DateTime.UtcNow,
                odometerKm = 1200,
                litres = 5m,
                totalCost = 20m,
                isFullTank = true
            }),
            ["maintenance get"] = await intruder.GetAsync($"/api/maintenances/{userMaintenance.Id}"),
            ["maintenance delete"] = await intruder.DeleteAsync($"/api/maintenances/mine/{userMaintenance.Id}"),
            ["maintenance by moto"] = await intruder.GetAsync($"/api/maintenances/mine/motorcycle/{moto.Id}"),
            ["records list"] = await intruder.GetAsync($"/api/maintenances/motorcycle/{moto.Id}/records"),
            ["upcoming"] = await intruder.GetAsync($"/api/maintenances/motorcycle/{moto.Id}/upcoming"),
            ["record create"] = await intruder.PostAsJsonAsync("/api/maintenances/records", new
            {
                motorcycleId = moto.Id,
                userMaintenanceId = userMaintenance.Id,
                performedAt = DateTime.UtcNow,
                performedKm = 1060
            }),
            ["attachments list"] = await intruder.GetAsync($"/api/maintenances/records/{recordId}/attachments"),
            ["book csv"] = await intruder.GetAsync($"/api/motorcycles/{moto.Id}/maintenance-book/book.csv"),
            ["book pdf"] = await intruder.GetAsync($"/api/motorcycles/{moto.Id}/maintenance-book/book.pdf"),
        };

        foreach (var (name, response) in attempts)
            Assert.True(denied.Contains(response.StatusCode), $"{name}: expected 403/404 but got {(int)response.StatusCode}");

        // Nothing leaked through and nothing was written by the intruder.
        var fuels = await owner.GetFromJsonAsync<List<IdResponse>>($"/api/fuel-ups/motorcycle/{moto.Id}");
        Assert.Single(fuels!);
        var records = await owner.GetFromJsonAsync<List<IdResponse>>($"/api/maintenances/motorcycle/{moto.Id}/records");
        Assert.Single(records!);
    }

    [RequiresDockerFact]
    public async Task Login_AfterMaxFailedAttempts_ShouldLockAccountAndReturn429()
    {
        // Its own client IP so this test does not spend the per-IP auth rate
        // limit of the rest of the suite (which shares the "unknown" partition).
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ClientIpStartupFilter.HeaderName, TestClientIps.Next());

        var email = $"locked-{Guid.NewGuid():N}@bikontrol.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123!",
            fullName = "Locked User"
        });
        register.EnsureSuccessStatusCode();

        var payload = new { email, password = "Secret123!" };
        // Register (1) + the 6 login calls below fit the per-IP auth budget
        // (10/min): every test gets its own partition via X-Test-Client-Ip,
        // so no shared-window workaround is needed.

        // Default limit is 5 failed attempts.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" });
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        // Correct credentials are rejected with 429 while locked out.
        var locked = await client.PostAsJsonAsync("/api/auth/login", payload);
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
    }

    [RequiresDockerFact]
    public async Task MaintenanceBook_ShouldReturnCsvAndPdfFiles()
    {
        var client = await RegisterClientAsync();
        var created = await CreateMotorcycleAsync(client);

        var csv = await client.GetAsync($"/api/motorcycles/{created.Id}/maintenance-book/book.csv");
        csv.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
        var csvBody = await csv.Content.ReadAsStringAsync();
        Assert.Contains("Fecha,Odometro", csvBody);

        var pdf = await client.GetAsync($"/api/motorcycles/{created.Id}/maintenance-book/book.pdf");
        pdf.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var pdfBytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }

    [RequiresDockerFact]
    public async Task Reminders_RequireAuthAndReturnDueList()
    {
        // Anonymous is rejected.
        var anonymous = _factory.CreateClient();
        var unauthorized = await anonymous.GetAsync("/api/reminders/due");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        // An authenticated user with no maintenance gets an empty list, not an error.
        var client = await RegisterClientAsync();
        var response = await client.GetAsync("/api/reminders/due");
        response.EnsureSuccessStatusCode();
        var due = await response.Content.ReadFromJsonAsync<List<DueReminderResponse>>();
        Assert.NotNull(due);
        Assert.Empty(due!);
    }

    [RequiresDockerFact]
    public async Task Refresh_ShouldRotateThroughTheHttpOnlyCookie()
    {
        // Server contract: registration issues the token as an HttpOnly cookie
        // and /refresh accepts it back from that cookie. The client handler
        // would not resend a Secure cookie over plain HTTP, so the test echoes
        // the Set-Cookie header itself - exactly what the browser does on https.
        var client = _factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}@bikontrol.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123!",
            fullName = "Refresh Cookie"
        });
        register.EnsureSuccessStatusCode();
        var setCookie = register.Headers.GetValues("Set-Cookie").First(v => v.StartsWith("bikontrol_rt="));

        var refreshClient = _factory.CreateClient();
        refreshClient.DefaultRequestHeaders.Add(ClientIpStartupFilter.HeaderName, TestClientIps.Next());
        refreshClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", setCookie);

        var refresh = await refreshClient.PostAsJsonAsync("/api/auth/refresh", new { });
        refresh.EnsureSuccessStatusCode();
        var session = await refresh.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.False(string.IsNullOrWhiteSpace(session!.Token));
        // The body stays clean: the rotated token rides the Set-Cookie header.
        Assert.Equal(string.Empty, session.RefreshToken);
    }

    private async Task<HttpClient> RegisterClientAsync()
    {
        var client = _factory.CreateClient();
        // Its own rate-limit partition (TestServer leaves the client IP null,
        // so every headerless request would share the "unknown" budget).
        client.DefaultRequestHeaders.Add(ClientIpStartupFilter.HeaderName, TestClientIps.Next());
        var email = $"user-{Guid.NewGuid():N}@bikontrol.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123!",
            fullName = "Integration User"
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static async Task<MotorcycleResponse> CreateMotorcycleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/motorcycles", new
        {
            name = "XTZ 150",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "La azul",
            km = 1000,
            displacement = 150,
            plate = $"ABC{Random.Shared.Next(100, 999)}",
            image = "default.png"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MotorcycleResponse>())!;
    }

    private sealed record IdResponse(Guid Id);
    private sealed record AuthResponse(string Token, string RefreshToken);
    private sealed record MotorcycleResponse(Guid Id, string Name, int Km);
    private sealed record DueReminderResponse(Guid UserMaintenanceId, string Name, bool IsOverdue);
}
