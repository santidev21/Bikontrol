using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bikontrol.Application.Interfaces;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// The anonymous one-click unsubscribe (RFC 8058) over the real API: a signed
/// token turns reminder emails off without a login, and a forged one is rejected.
/// </summary>
[Collection("api")]
public sealed class EmailUnsubscribeIntegrationTests
{
    private readonly PostgresApiFactory _factory;

    public EmailUnsubscribeIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task ValidToken_TurnsRemindersOff_AndIsIdempotent()
    {
        var (client, userId) = await RegisterAsync();
        var token = _factory.Services.GetRequiredService<IUnsubscribeTokenService>().Create(userId);

        var anonymous = NewAnonymousClient();
        var first = await anonymous.PostAsync($"/api/reminders/email/unsubscribe?token={Uri.EscapeDataString(token)}", null);
        var second = await anonymous.PostAsync($"/api/reminders/email/unsubscribe?token={Uri.EscapeDataString(token)}", null);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/users/me");
        Assert.False(profile.GetProperty("remindersEnabled").GetBoolean());
    }

    [RequiresDockerFact]
    public async Task ForgedOrMissingToken_ReturnsBadRequest()
    {
        var anonymous = NewAnonymousClient();

        var forged = await anonymous.PostAsync("/api/reminders/email/unsubscribe?token=AAAA.BBBB", null);
        var missing = await anonymous.PostAsync("/api/reminders/email/unsubscribe", null);

        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var body = await forged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_unsubscribe_token", body.GetProperty("code").GetString());
    }

    private HttpClient NewAnonymousClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ClientIpStartupFilter.HeaderName, TestClientIps.Next());
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> RegisterAsync()
    {
        var client = NewAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"user-{Guid.NewGuid():N}@bikontrol.test",
            password = "Secret123!",
            fullName = "Unsubscribe User"
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var me = await client.GetFromJsonAsync<ProfileResponse>("/api/users/me");
        return (client, me!.Id);
    }

    private sealed record AuthResponse(string Token, string RefreshToken);

    private sealed record ProfileResponse(Guid Id);
}
