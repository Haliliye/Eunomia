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
            _logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);

            var (statusCode, message) = ex switch
            {
                ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    string.Join(" ", validationEx.Errors.Select(e => e.ErrorMessage))),
                AuthenticationFailedException => (HttpStatusCode.Unauthorized, ex.Message),
                ConcurrencyConflictException => (HttpStatusCode.Conflict, ex.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
