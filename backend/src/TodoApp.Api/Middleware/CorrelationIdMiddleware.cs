using Serilog.Context;

namespace TodoApp.Api.Middleware;

/// <summary>
/// Gives every request a correlation id — echoes one back if the client
/// (or an upstream proxy) already sent X-Correlation-Id, otherwise
/// generates a fresh one. Pushed into Serilog's LogContext so every log
/// line for this request carries it (visible in Render's log search), and
/// echoed back in the response header so a client/support conversation can
/// reference "look up X-Correlation-Id: ..." instead of a timestamp guess.
/// Previously absent entirely — found in the 2026-08-11 review.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>Reads back the id CorrelationIdMiddleware stashed for this request — used by ExceptionHandlingMiddleware to include it in the error body.</summary>
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue("CorrelationId", out var value) ? value as string : null;
}
