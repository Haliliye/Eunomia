using System.Net;
using FluentValidation;
using TodoApp.Application.Common;

namespace TodoApp.Api.Middleware;

/// <summary>
/// Translates exceptions from Application/Domain into proper HTTP status
/// codes instead of a raw 500. Keeps handlers free of try/catch — they
/// just throw the exception type that matches the failure.
/// </summary>
public class ExceptionHandlingMiddleware
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
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.GetCorrelationId();
            _logger.LogError(ex, "Unhandled exception while processing {Path} (CorrelationId: {CorrelationId})", context.Request.Path, correlationId);

            var (statusCode, title, message) = ex switch
            {
                ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    "Validation failed",
                    string.Join(" ", validationEx.Errors.Select(e => e.ErrorMessage))),
                AuthenticationFailedException => (HttpStatusCode.Unauthorized, "Authentication failed", ex.Message),
                ConcurrencyConflictException => (HttpStatusCode.Conflict, "Concurrency conflict", ex.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Not found", ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden", ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, "Invalid request", ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid request", ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred", "An unexpected error occurred.")
            };

            // RFC 7807 (application/problem+json) shape — type/title/status/
            // detail/instance are the standard fields — plus a correlationId
            // extension member so a support conversation can reference one
            // id that also appears throughout the server logs for this
            // request. `error` is kept alongside for backward compatibility:
            // the frontend already reads response.data.error everywhere, and
            // migrating every one of those call sites isn't worth doing in
            // the same change as adding the standard shape.
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                type = $"https://httpstatuses.com/{(int)statusCode}",
                title,
                status = (int)statusCode,
                detail = message,
                instance = context.Request.Path.Value,
                correlationId,
                error = message,
            });
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
