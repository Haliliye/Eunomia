namespace TodoApp.Api.Middleware;

/// <summary>
/// Establishes an API versioning policy without restructuring a single
/// existing route — found missing in the 2026-08-11 review, but a URL-segment
/// scheme (e.g. rewriting every controller to /api/v1/...) would touch every
/// controller attribute across the whole surface with no compiler available
/// to catch a mistake, for a benefit (multiple concurrently-supported API
/// versions) this single-frontend app doesn't need yet. This is the
/// deliberately low-risk middle ground: every response carries an
/// X-Api-Version header, and GET /api/version returns the same value as
/// JSON — so a version is genuinely being communicated to clients, and the
/// day a real breaking change is needed, introducing /api/v2/... alongside
/// the existing (implicitly v1) routes is additive, not a rewrite.
/// </summary>
public class ApiVersionMiddleware
{
    public const string CurrentVersion = "1.0";

    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Api-Version"] = CurrentVersion;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class ApiVersionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiVersionHeader(this IApplicationBuilder app) =>
        app.UseMiddleware<ApiVersionMiddleware>();
}
