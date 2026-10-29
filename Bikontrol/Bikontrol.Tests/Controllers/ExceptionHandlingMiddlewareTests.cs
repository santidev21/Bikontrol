using System.Text.Json;
using Bikontrol.API.Middleware;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Bikontrol.Tests.Controllers;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAlwaysEchoRequestIdHeader()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("trace-123", context.Response.Headers["X-Request-Id"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthException_ShouldMapStatusCodeAndMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new AuthException("Bloqueada", 429),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(429, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Bloqueada", doc.RootElement.GetProperty("error").GetString());
        // The envelope carries a stable machine code alongside the message.
        Assert.Equal("auth_error", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthExceptionHasCode_ShouldEchoTheCode()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new AuthException("El correo o contraseña son inválidos.", 401, "invalid_credentials"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("invalid_credentials", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpected_ShouldReturn500WithGenericMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        // The message must not leak internals.
        Assert.Equal("Error inesperado en el servidor.", doc.RootElement.GetProperty("error").GetString());
        Assert.Equal("server_error", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseAlreadyStarted_ShouldNotRewriteIt()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(
            new StartedResponseFeature());
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("too late"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        // Status stays the default because rewriting a started response is illegal.
        Assert.Equal(200, context.Response.StatusCode);
    }

    private sealed class StartedResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    [Theory]
    [InlineData("23505", 409, "duplicate")]
    [InlineData("22001", 400, "validation")]
    [InlineData("23514", 400, "validation")]
    public async Task InvokeAsync_WhenDatabaseRejectsTheData_ShouldMapToClientError(string sqlState, int status, string code)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var pg = new PostgresException("rejected", "ERROR", "ERROR", sqlState);
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("save failed", pg),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(status, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(code, doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenDatabaseFailsForAnotherReason_ShouldStayAServerError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var pg = new PostgresException("down", "ERROR", "ERROR", "57P01");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("save failed", pg),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTheClientAborted_ShouldNotReportAServerError()
    {
        using var cts = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cts.Token };
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAnswerWithProblemDetailsKeepingTheLegacyFields()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("Moto no encontrada", "motorcycle_not_found"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        var root = doc.RootElement;
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("motorcycle_not_found", root.GetProperty("code").GetString());
        Assert.Equal("urn:bikontrol:error:motorcycle_not_found", root.GetProperty("type").GetString());
        Assert.Equal("Moto no encontrada", root.GetProperty("detail").GetString());
        Assert.Equal("Moto no encontrada", root.GetProperty("error").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task InvokeAsync_WhenTheBodyIsTooLarge_ShouldAnswer413WithAStableCode()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new Microsoft.AspNetCore.Http.BadHttpRequestException("too big", StatusCodes.Status413PayloadTooLarge),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(413, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("payload_too_large", doc.RootElement.GetProperty("code").GetString());
    }
}
