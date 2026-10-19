using Bikontrol.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Bikontrol.API.Middleware
{
    public partial class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Correlate the error with the logs (and with Serilog's trace id):
            // echo the id back so a user report maps to a log/metric entry.
            var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
            context.Response.Headers["X-Request-Id"] = requestId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger, requestId);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            ILogger logger,
            string requestId)
        {
            if (context.Response.HasStarted)
            {
                // La respuesta ya empezó a enviarse: no se puede reescribir el
                // status/body sin romper el stream, así que solo se registra.
                LogAfterResponseStarted(logger, exception, requestId, exception.Message);
                return;
            }

            HttpStatusCode statusCode;
            string message;
            string code;

            switch (exception)
            {
                case AuthException authEx:
                    statusCode = (HttpStatusCode)authEx.StatusCode;
                    message = authEx.Message;
                    code = authEx.Code ?? "auth_error";
                    break;

                case NotFoundException notFoundEx:
                    statusCode = HttpStatusCode.NotFound;
                    message = notFoundEx.Message;
                    code = notFoundEx.Code ?? "not_found";
                    break;

                case ForbiddenAccessException forbiddenEx:
                    statusCode = HttpStatusCode.Forbidden;
                    message = forbiddenEx.Message;
                    code = forbiddenEx.Code ?? "forbidden";
                    break;

                case ValidationException validationEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = validationEx.Message;
                    code = validationEx.Code ?? "validation";
                    break;

                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = "Los datos cambiaron mientras los editabas. Recarga e inténtalo de nuevo.";
                    code = "conflict";
                    LogConcurrencyConflict(logger, exception, requestId, exception.Message);
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "Error inesperado en el servidor.";
                    code = "server_error";
                    LogUnhandledException(logger, exception, requestId, exception.Message);
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // The code is the machine-stable part of the envelope: the frontend
            // can translate known codes instead of showing the hardcoded
            // (Spanish) message, which is what unlocks non-Spanish UIs.
            var response = new { error = message, code };
            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json, context.RequestAborted);
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Exception after response started [{RequestId}]: {Message}")]
        private static partial void LogAfterResponseStarted(ILogger logger, Exception exception, string requestId, string message);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Concurrency conflict [{RequestId}]: {Message}")]
        private static partial void LogConcurrencyConflict(ILogger logger, Exception exception, string requestId, string message);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Unhandled exception [{RequestId}]: {Message}")]
        private static partial void LogUnhandledException(ILogger logger, Exception exception, string requestId, string message);
    }
}
