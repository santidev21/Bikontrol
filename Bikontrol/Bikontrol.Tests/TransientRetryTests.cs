using Bikontrol.Infrastructure.Email;
using Bikontrol.Infrastructure.Notifications;
using Bikontrol.Infrastructure.Resilience;
using MailKit;
using MailKit.Net.Smtp;
using System.Net.Sockets;

namespace Bikontrol.Tests;

public class TransientRetryTests
{
    private static Task NoDelay(TimeSpan _, CancellationToken __) => Task.CompletedTask;

    [Fact]
    public async Task ExecuteAsync_WhenTransientFailuresClear_ShouldReturnTheResult()
    {
        var calls = 0;

        var result = await TransientRetry.ExecuteAsync(
            _ => ++calls < 3 ? throw new TimeoutException() : Task.FromResult("ok"),
            e => e is TimeoutException,
            delay: NoDelay);

        Assert.Equal("ok", result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttemptsRunOut_ShouldThrowTheLastError()
    {
        var calls = 0;

        await Assert.ThrowsAsync<TimeoutException>(() => TransientRetry.ExecuteAsync<int>(
            _ => { calls++; throw new TimeoutException(); },
            e => e is TimeoutException,
            maxAttempts: 3,
            delay: NoDelay));

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheErrorIsNotTransient_ShouldNotRetry()
    {
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => TransientRetry.ExecuteAsync<int>(
            _ => { calls++; throw new InvalidOperationException(); },
            e => e is TimeoutException,
            delay: NoDelay));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBackOffExponentially()
    {
        var waits = new List<TimeSpan>();

        await Assert.ThrowsAsync<TimeoutException>(() => TransientRetry.ExecuteAsync<int>(
            _ => throw new TimeoutException(),
            _ => true,
            maxAttempts: 4,
            baseDelay: TimeSpan.FromMilliseconds(100),
            delay: (d, _) => { waits.Add(d); return Task.CompletedTask; }));

        Assert.Equal([100, 200, 400], waits.Select(w => (int)w.TotalMilliseconds));
    }

    [Fact]
    public void Smtp_IsTransient_ShouldRetryNetworkAndTemporaryErrorsOnly()
    {
        Assert.True(SmtpEmailSender.IsTransient(new TimeoutException()));
        Assert.True(SmtpEmailSender.IsTransient(new SocketException()));
        Assert.True(SmtpEmailSender.IsTransient(new IOException()));
        Assert.True(SmtpEmailSender.IsTransient(new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, SmtpStatusCode.MailboxBusy, "busy")));
        Assert.False(SmtpEmailSender.IsTransient(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "no such user")));
        Assert.False(SmtpEmailSender.IsTransient(new MailKit.Security.AuthenticationException()));
        Assert.False(SmtpEmailSender.IsTransient(new InvalidOperationException()));
    }

    [Fact]
    public void WebPush_IsTransient_ShouldRetryServerErrorsAndThrottlingOnly()
    {
        Assert.True(WebPushSender.IsTransient(new HttpRequestException()));
        Assert.True(WebPushSender.IsTransient(new WebPush.WebPushException("x", null!, new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable))));
        Assert.True(WebPushSender.IsTransient(new WebPush.WebPushException("x", null!, new HttpResponseMessage((System.Net.HttpStatusCode)429))));
        Assert.False(WebPushSender.IsTransient(new WebPush.WebPushException("x", null!, new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest))));
        Assert.False(WebPushSender.IsTransient(new InvalidOperationException()));
    }
}
