namespace TodoApp.Api.Middleware;

/// <summary>
/// Adds the standard defensive HTTP response headers a production API
/// should send — none of these were previously set at all (found in the
/// 2026-08-11 security review). This is a pure API (JSON responses, no
/// server-rendered HTML), so the policy here is deliberately strict: the
/// app never needs to be framed, never serves third-party scripts/styles,
/// and clients should never guess a MIME type. Swagger's own UI (dev-only)
/// is excluded since it needs inline scripts/styles to render.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // This API is never meant to be embedded in an <iframe> anywhere.
            headers["X-Frame-Options"] = "DENY";
            // Stops the browser from MIME-sniffing a response into executing
            // as something other than its declared Content-Type.
            headers["X-Content-Type-Options"] = "nosniff";
            // Legacy header, but still expected by some scanners/clients; the
            // real protection is CSP below.
            headers["X-XSS-Protection"] = "0";
            // Don't leak the full referring URL (which can contain ids/tokens
            // in query strings) to a different origin.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // No third-party embeds/geolocation/etc. — a pure JSON API has no
            // use for any browser feature this controls.
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            if (!isSwagger)
            {
                // default-src 'none' + frame-ancestors 'none': this origin
                // serves JSON, not HTML, so there's nothing here a CSP needs
                // to allow loading.
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
