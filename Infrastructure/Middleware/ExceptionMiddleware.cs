using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace NoteApi.Infrastructure.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception has occurred. CorrelationId: {CorrelationId}", context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "An internal server error occurred.",
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = context.Request.Path
            };

            problemDetails.Extensions.Add("correlationId", context.TraceIdentifier);

            var json = JsonSerializer.Serialize(problemDetails);
            return context.Response.WriteAsync(json);
        }
    }
}
