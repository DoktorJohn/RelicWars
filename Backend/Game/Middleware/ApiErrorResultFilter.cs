using Game.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Game.Middleware
{
    public class ApiErrorResultFilter : IAsyncAlwaysRunResultFilter
    {
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var statusCode = GetStatusCode(context.Result);
            if (statusCode < 400) return next();

            var currentValue = (context.Result as ObjectResult)?.Value;
            if (currentValue is ApiError) return next();

            var (code, defaultMessage) = GetError(statusCode);
            var message = statusCode < 500 && currentValue is string text && !string.IsNullOrWhiteSpace(text)
                ? text
                : defaultMessage;

            context.Result = new ObjectResult(new ApiError(code, message))
            {
                StatusCode = statusCode
            };

            return next();
        }

        private static int GetStatusCode(IActionResult result)
        {
            return result switch
            {
                ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
                StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
                _ => StatusCodes.Status200OK
            };
        }

        private static (string Code, string Message) GetError(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => ("request.invalid", "Anmodningen er ugyldig."),
                StatusCodes.Status401Unauthorized => ("auth.unauthorized", "Login er påkrævet."),
                StatusCodes.Status403Forbidden => ("auth.forbidden", "Du har ikke adgang til handlingen."),
                StatusCodes.Status404NotFound => ("resource.not_found", "Ressourcen blev ikke fundet."),
                StatusCodes.Status409Conflict => ("resource.conflict", "Handlingen er i konflikt med den aktuelle tilstand."),
                _ => ("server.error", "En intern serverfejl opstod.")
            };
        }
    }
}
