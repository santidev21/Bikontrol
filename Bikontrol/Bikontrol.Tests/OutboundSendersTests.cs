using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebPush;
using PushSubscriptionEntity = Bikontrol.Domain.Entities.PushSubscription;

namespace Bikontrol.Tests;

public class OutboundSendersTests
{
    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static SmtpEmailSender Smtp(SmtpOptions options, string environment = "Production") =>
        new(Options.Create(options), NullLogger<SmtpEmailSender>.Instance, new FakeEnvironment(environment));

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Smtp_WhenNotConfigured_ShouldSkipWithoutThrowing(string environment)
    {
        var exception = await Record.ExceptionAsync(() =>
            Smtp(new SmtpOptions(), environment).SendAsync("rider@example.com", "Subject", "Body"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Smtp_WhenAddressIsInvalid_ShouldFailBeforeTouchingTheNetwork()
    {
        var sender = Smtp(new SmtpOptions { Host = "127.0.0.1", Username = "u", Password = "p" });

        await Assert.ThrowsAnyAsync<Exception>(() => sender.SendAsync("not an address", "Subject", "Body"));
    }

    [Fact]
    public async Task Smtp_WhenServerIsUnreachable_ShouldSurfaceTheFailureAfterRetrying()
    {
        var sender = Smtp(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = 1,
            EnableSsl = false,
            Username = "u",
            Password = "p",
            FromEmail = "noreply@example.com",
            TimeoutSeconds = 1
        });

        await Assert.ThrowsAnyAsync<Exception>(() => sender.SendAsync("rider@example.com", "Subject", "Body"));
    }

    [Fact]
    public void IsTransient_ShouldCoverProtocolAndCancellationFailures()
    {
        Assert.True(SmtpEmailSender.IsTransient(new MailKit.Net.Smtp.SmtpProtocolException("dropped")));
        Assert.True(SmtpEmailSender.IsTransient(new OperationCanceledException()));
        Assert.True(WebPushSender.IsTransient(new TaskCanceledException()));
        Assert.False(WebPushSender.IsTransient(new InvalidOperationException()));
    }

    [Fact]
    public async Task WebPush_WhenNotConfigured_ShouldKeepTheSubscription()
    {
        var sender = new WebPushSender(Options.Create(new WebPushOptions()), NullLogger<WebPushSender>.Instance);

        var keep = await sender.SendAsync(new PushSubscriptionEntity(), new PushPayload { Title = "t" });

        Assert.True(keep);
    }

    [Theory]
    [InlineData(HttpStatusCode.Created, true)]
    [InlineData(HttpStatusCode.Gone, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public async Task WebPush_ShouldReportWhetherTheSubscriptionSurvives(HttpStatusCode status, bool expected)
    {
        using var listener = StartListener(status, out var endpoint);
        var sender = ConfiguredWebPush();

        var keep = await sender.SendAsync(Subscription(endpoint), new PushPayload { Title = "Due", Body = "Oil", Url = "/dashboard" });

        Assert.Equal(expected, keep);
    }

    [Fact]
    public async Task WebPush_WhenPushServiceRejectsTheRequest_ShouldThrowWithoutRetrying()
    {
        using var listener = StartListener(HttpStatusCode.BadRequest, out var endpoint);
        var sender = ConfiguredWebPush();

        await Assert.ThrowsAsync<WebPushException>(() =>
            sender.SendAsync(Subscription(endpoint), new PushPayload { Title = "Due" }));
    }

    private static WebPushSender ConfiguredWebPush()
    {
        var keys = VapidHelper.GenerateVapidKeys();
        return new WebPushSender(
            Options.Create(new WebPushOptions { PublicKey = keys.PublicKey, PrivateKey = keys.PrivateKey }),
            NullLogger<WebPushSender>.Instance);
    }

    private static PushSubscriptionEntity Subscription(string endpoint)
    {
        using var ecdh = ECDiffieHellman.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var p = ecdh.ExportParameters(false);
        var point = new byte[65];
        point[0] = 0x04;
        p.Q.X!.CopyTo(point, 1);
        p.Q.Y!.CopyTo(point, 33);

        return new PushSubscriptionEntity
        {
            Endpoint = endpoint,
            P256dh = Base64Url(point),
            Auth = Base64Url(RandomNumberGenerator.GetBytes(16))
        };
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>A loopback push service that answers every request with <paramref name="status"/>.</summary>
    private static HttpListener StartListener(HttpStatusCode status, out string endpoint)
    {
        // Let the OS pick a free port, then retry if another test or process grabs
        // it between releasing it here and HttpListener binding it (a fixed random
        // range collided on CI: "Address already in use").
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var candidate = new HttpListener();
            endpoint = $"http://127.0.0.1:{port}/push/";
            candidate.Prefixes.Add(endpoint);
            try
            {
                candidate.Start();
            }
            catch (HttpListenerException) when (attempt < maxAttempts)
            {
                candidate.Close();
                continue;
            }

            return ServeInBackground(candidate, status);
        }
    }

    private static HttpListener ServeInBackground(HttpListener listener, HttpStatusCode status)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    context.Response.StatusCode = (int)status;
                    context.Response.Close();
                }
            }
            catch (Exception) when (!listener.IsListening)
            {
                // Listener disposed by the test.
            }
        });
        return listener;
    }
}
