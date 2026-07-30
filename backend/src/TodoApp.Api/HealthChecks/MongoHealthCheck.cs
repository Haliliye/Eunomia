using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.HealthChecks;

/// <summary>
/// Pings MongoDB with a trivial command — used so Docker Compose (and any
/// orchestrator) can tell "the process started" apart from "the process can
/// actually reach its database", which `depends_on` alone can't do.
/// </summary>
public class MongoHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;

    public MongoHealthCheck(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Teams.Database.RunCommandAsync((Command<object>)"{ ping: 1 }", cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is not reachable.", ex);
        }
    }
}
