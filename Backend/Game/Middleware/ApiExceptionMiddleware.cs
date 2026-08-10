using Game.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Game.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, code, message) = exception switch
            {
                UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "auth.forbidden", "Du har ikke adgang til handlingen."),
                KeyNotFoundException => ((int)HttpStatusCode.NotFound, "resource.not_found", "Ressourcen blev ikke fundet."),
                ArgumentException => ((int)HttpStatusCode.BadRequest, "request.invalid", "Anmodningen er ugyldig."),
                DbUpdateConcurrencyException => ((int)HttpStatusCode.Conflict, "resource.concurrent_update", "Ressourcen blev ændret af en anden handling. Prøv igen."),
                InvalidOperationException => ((int)HttpStatusCode.Conflict, "resource.conflict", "Handlingen er i konflikt med den aktuelle tilstand."),
                _ => ((int)HttpStatusCode.InternalServerError, "server.error", "En intern serverfejl opstod.")
            };

            if (exception is DbUpdateConcurrencyException concurrencyException)
            {
                _logger.LogWarning(
                    exception,
                    "Handled EF concurrency exception. Entries: {ConcurrencyEntries}",
                    DescribeConcurrencyEntries(concurrencyException));
            }
            else if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled API exception");
            }
            else
            {
                _logger.LogWarning(exception, "Handled API exception");
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiError(code, message));
        }

        private static string DescribeConcurrencyEntries(DbUpdateConcurrencyException exception)
        {
            if (exception.Entries.Count == 0)
            {
                return "none";
            }

            return string.Join("; ", exception.Entries.Select(entry =>
            {
                var primaryKey = entry.Metadata.FindPrimaryKey();
                string key = primaryKey == null
                    ? "<no key>"
                    : string.Join(", ", primaryKey.Properties.Select(property =>
                        $"{property.Name}={entry.Property(property.Name).CurrentValue ?? "<null>"}"));
                return $"{entry.Metadata.DisplayName()} [{key}] State={entry.State}";
            }));
        }
    }
}
