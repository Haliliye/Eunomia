using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TodoApp.Api.BackgroundServices;
using TodoApp.Api.HealthChecks;
using TodoApp.Api.Middleware;
using TodoApp.Api.Realtime;
using TodoApp.Application;
using TodoApp.Application.Common;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.Security;

// Bootstrap logger — active only until the host builds its real one below,
// so startup failures (e.g. the Jwt:SecretKey check further down) are still logged.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// WebApplication.CreateBuilder wires up appsettings.json with
// reloadOnChange: true by default, which starts a FileSystemWatcher
// (inotify on Linux). That's known to segfault (exit code 139) in some
// restricted container environments — Render's free tier included — and
// we don't need it anyway: every real setting here comes from environment
// variables (Docker Compose / Render's dashboard), not a JSON file that
// changes while the container is running. Re-adding the same JSON files
// with reloadOnChange: false avoids starting the watcher at all.
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// CreateBuilder normally adds this automatically for Development — restoring
// it since Sources.Clear() above wiped it out too. Local dev (Visual Studio)
// relies on `dotnet user-secrets set ...` for Jwt/Smtp/R2Storage values.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// Structured logging (Serilog) instead of the default provider — console for
// local dev, plus a rolling file so history survives past the terminal
// scrolling away. Swap/add sinks (Seq, Application Insights, etc.) here for
// a real deployment without touching any handler/controller code.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File("logs/todoapp-.log", rollingInterval: RollingInterval.Day));

// CQRS/MediatR + FluentValidation registrations live in Application.
builder.Services.AddApplication();

// MongoDB repositories + auth (password hashing, JWT) live in Infrastructure.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

// Fail fast rather than starting up with an empty/weak signing key — this
// used to be a hardcoded placeholder committed to appsettings.json, which
// is exactly the kind of secret that shouldn't be in source control.
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey is missing or shorter than 32 characters. Set a long random value via " +
        "'dotnet user-secrets set \"Jwt:SecretKey\" \"<value>\"' for local development, or the " +
        "Jwt__SecretKey environment variable elsewhere (see README's DevOps section).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        };

        // Browsers can't attach an Authorization header to a WebSocket
        // handshake, so SignalR clients send the JWT as ?access_token=...
        // instead — this reads it back out for requests under /hubs. Regular
        // API calls now ride on an httpOnly cookie instead of a
        // JS-attached header — read from there when no header was sent.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessTokenFromQuery = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessTokenFromQuery) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessTokenFromQuery;
                    return Task.CompletedTask;
                }

                if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Every endpoint requires a valid JWT by default; AuthController opts out
// with [AllowAnonymous] for register/login.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Rate limiting on auth endpoints specifically — login/register/refresh had
// zero brute-force protection before this. Keyed per client IP so one
// attacker can't exhaust the limit for everyone else.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste just the token — Swagger adds the 'Bearer ' prefix."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Comma-separated additional origins (e.g. the production frontend URL) can
// be supplied via Cors:AdditionalOrigins / CORS__ADDITIONALORIGINS — keeps
// the Vercel/Netlify URL out of source, settable per-environment instead.
var additionalCorsOrigins = builder.Configuration["Cors:AdditionalOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

var corsOrigins = new[] { "http://localhost:5173", "https://eunomia-seven.vercel.app" }
    .Concat(additionalCorsOrigins)
    .Distinct()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR's negotiate/websocket handshake needs this
    });
});

// So Docker Compose (and any orchestrator) can tell "the process started"
// apart from "the process can actually reach MongoDB".
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>("mongodb");

// US-120: periodically checks for stories due soon and reminds the assignee.
builder.Services.AddHostedService<DueDateReminderBackgroundService>();

var app = builder.Build();

// Idempotent — safe to run on every startup. Creates the indexes that back
// team/story lookups, US-116 keyword search, and login-by-email.
using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<TodoApp.Infrastructure.Persistence.MongoDbContext>();
    await TodoApp.Infrastructure.Persistence.MongoIndexInitializer.EnsureIndexesAsync(mongoContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseExceptionHandling();

// Skipped in Development: the Docker Compose setup runs the API over plain
// HTTP (no self-signed cert wiring inside the container), and redirecting
// would break it. Doesn't affect the Windows/Visual Studio workflow, which
// already calls https://localhost:5001 directly.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AppHub>("/hubs/app");
// AllowAnonymous is required here — the global FallbackPolicy above makes
// every endpoint require a JWT by default, but health checks are polled by
// infrastructure (Render's own health probe, uptime monitors, load
// balancers) that has no way to authenticate. Without this, /health itself
// returns 401, which most platforms then treat as "service unhealthy."
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Top-level statements generate an internal Program class by default — this
// makes it public so WebApplicationFactory<Program> in TodoApp.IntegrationTests
// (a different assembly) can actually reference it.
public partial class Program { }
