using Microsoft.Extensions.Options;
using TodoApp.Application.Integrations.Jira;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;
using TodoApp.Infrastructure.Integrations.Jira;

namespace TodoApp.Api.BackgroundServices;

/// <summary>
/// Periodically re-imports every team with AutoSyncEnabled on its
/// JiraProjectSync record — see that class for why this is opt-in rather
/// than automatic for every Jira-linked team. Reuses JiraProjectImportService,
/// the exact same code path a manual "Import a project…" click runs, so
/// auto-sync behaves identically to a manual re-import (create-or-update by
/// JiraIssueKey, comments/attachments de-duped, etc.) — it's just triggered
/// on a timer instead of a click.
/// </summary>
public class JiraAutoSyncBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JiraSettings _jiraSettings;
    private readonly ILogger<JiraAutoSyncBackgroundService> _logger;

    public JiraAutoSyncBackgroundService(IServiceScopeFactory scopeFactory, IOptions<JiraSettings> jiraSettings, ILogger<JiraAutoSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _jiraSettings = jiraSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Jira isn't necessarily configured at all (Jira:ClientId/ClientSecret
        // are optional deploy-time settings) — no point looping just to fail
        // every cycle if it isn't.
        if (!_jiraSettings.IsConfigured) return;

        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // A single failed cycle (e.g. a transient Mongo/Jira hiccup)
                // shouldn't kill the whole background service — log and try
                // again next interval.
                _logger.LogError(ex, "Jira auto-sync cycle failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var syncRepository = scope.ServiceProvider.GetRequiredService<IJiraProjectSyncRepository>();
        var teamRepository = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        var accessTokenProvider = scope.ServiceProvider.GetRequiredService<JiraAccessTokenProvider>();
        var importService = scope.ServiceProvider.GetRequiredService<JiraProjectImportService>();

        var syncs = await syncRepository.GetAllAutoSyncEnabledAsync(cancellationToken);

        foreach (var sync in syncs)
        {
            try
            {
                var team = await teamRepository.GetByIdAsync(sync.TeamId, cancellationToken);
                if (team is null) continue; // team was deleted since — nothing to sync into

                var (connection, accessToken) = await accessTokenProvider.GetValidAccessTokenAsync(sync.ConnectedByUserId, cancellationToken);
                // setAutoSync: null — a background sync run shouldn't change
                // the setting itself, only refresh data and LastSyncedOn.
                await importService.ImportAsync(team, sync.ProjectKey, accessToken, connection.CloudId, sync.ConnectedByUserId, setAutoSync: null, cancellationToken);
            }
            catch (Exception ex)
            {
                // Most likely cause: ConnectedByUserId's Jira connection
                // expired with no refresh token available (see
                // JiraAccessTokenProvider) — the team just silently stops
                // getting fresh data until someone reconnects and re-imports,
                // rather than this loop failing loudly on every cycle.
                _logger.LogError(ex, "Jira auto-sync failed for team {TeamId} (project {ProjectKey})", sync.TeamId, sync.ProjectKey);
            }
        }
    }
}
