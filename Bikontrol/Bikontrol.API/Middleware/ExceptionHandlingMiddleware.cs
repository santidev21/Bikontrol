using Bikontrol.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Net;

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

            if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                // The client went away: nothing to answer and nothing to alarm on.
                context.Response.StatusCode = ClientClosedRequest;
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

                case BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }:
                    statusCode = HttpStatusCode.RequestEntityTooLarge;
                    message = ErrorResponse.ForStatus(StatusCodes.Status413PayloadTooLarge)!.Value.Message;
                    code = "payload_too_large";
                    break;

                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = "Los datos cambiaron mientras los editabas. Recarga e inténtalo de nuevo.";
                    code = "conflict";
                    LogConcurrencyConflict(logger, exception, requestId, exception.Message);
                    break;

                case DbUpdateException { InnerException: PostgresException pg } when IsClientDataError(pg.SqlState):
                    // A race on a unique index, or a value the schema rejects, is
                    // the caller's data, not a server fault.
                    if (pg.SqlState == PostgresErrorCodes.UniqueViolation)
                    {
                        statusCode = HttpStatusCode.Conflict;
                        message = "Ya existe un registro con esos datos.";
                        code = "duplicate";
                    }
                    else
                    {
                        statusCode = HttpStatusCode.BadRequest;
                        message = "Alguno de los datos enviados no es válido.";
                        code = "validation";
                    }
                    LogDataConflict(logger, exception, requestId, pg.SqlState);
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "Error inesperado en el servidor.";
                    code = "server_error";
                    LogUnhandledException(logger, exception, requestId, exception.Message);
                    break;
            }

            // The code is the machine-stable part of the envelope: the frontend
            // can translate known codes instead of showing the hardcoded
            // (Spanish) message, which is what unlocks non-Spanish UIs.
            await ErrorResponse.WriteAsync(context, (int)statusCode, code, message);
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Exception after response started [{RequestId}]: {Message}")]
        private static partial void LogAfterResponseStarted(ILogger logger, Exception exception, string requestId, string message);

        private const int ClientClosedRequest = 499;

        private static bool IsClientDataError(string sqlState) =>
            sqlState is PostgresErrorCodes.UniqueViolation
                or PostgresErrorCodes.StringDataRightTruncation
                or PostgresErrorCodes.CheckViolation
                or PostgresErrorCodes.NotNullViolation;

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Database rejected the data [{RequestId}]: SQLSTATE {SqlState}")]
        private static partial void LogDataConflict(ILogger logger, Exception exception, string requestId, string sqlState);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Concurrency conflict [{RequestId}]: {Message}")]
        private static partial void LogConcurrencyConflict(ILogger logger, Exception exception, string requestId, string message);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Unhandled exception [{RequestId}]: {Message}")]
        private static partial void LogUnhandledException(ILogger logger, Exception exception, string requestId, string message);
    }
}
