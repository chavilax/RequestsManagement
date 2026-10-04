using System.Text.Json;
using RequestsManagement.BL.Exceptions;

namespace RequestsManagement.WebApi.Middleware
{
    /// <summary>
    /// טיפול מרכזי בשגיאות - ממפה exceptions של שכבת ה-BL לקודי HTTP מתאימים
    /// ומחזיר תשובת ProblemDetails אחידה ללקוח.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
                ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            // שגיאות לא צפויות (500) נרשמות כ-Error. שגיאות עסקיות צפויות - כ-Warning.
            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception");
            else
                _logger.LogWarning("{Title}: {Message}", title, ex.Message);

            var problem = new
            {
                type = $"https://httpstatuses.io/{statusCode}",
                title,
                status = statusCode,
                // מידע רגיש לא נחשף ללקוח ב-500 - רק הודעה גנרית
                detail = statusCode == StatusCodes.Status500InternalServerError ? null : ex.Message
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
