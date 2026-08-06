using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Common;
using TodoApp.Application.Integrations.Jira.Commands;
using TodoApp.Application.Integrations.Jira.Queries;
using TodoApp.Infrastructure.Integrations.Jira;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/integrations/jira")]
public class IntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JiraSettings _jiraSettings;

    public IntegrationsController(IMediator mediator, IOptions<JiraSettings> jiraSettings)
    {
        _mediator = mediator;
        _jiraSettings = jiraSettings.Value;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetJiraStatusQuery(User.GetUserId()), cancellationToken);
        return Ok(status);
    }

    /// <summary>Returns the URL for the frontend to do a full-page redirect to — Atlassian's consent screen can't run inside an XHR or an iframe.</summary>
    [HttpGet("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var authorizationUrl = await _mediator.Send(new StartJiraConnectionCommand(User.GetUserId()), cancellationToken);
        return Ok(new { authorizationUrl });
    }

    /// <summary>
    /// Atlassian redirects the user's browser here directly — no JWT is sent
    /// (it's a plain top-level navigation, not our SPA's authenticated
    /// fetch), so this must be anonymous. The user's identity instead comes
    /// from the encrypted "state" value we handed Atlassian in Connect().
    /// Always ends in a redirect back to the frontend, success or failure,
    /// so the user never sees a bare API response.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect(BuildFrontendRedirect(success: false, "The Jira authorization was cancelled or denied."));

        var result = await _mediator.Send(new CompleteJiraConnectionCommand(code, state), cancellationToken);
        return Redirect(BuildFrontendRedirect(result.Success, result.ErrorMessage));
    }

    private string BuildFrontendRedirect(bool success, string? errorMessage)
    {
        // Lands on the existing single-page Settings screen (its Jira card
        // reads these query params on mount) — no separate /integrations
        // route needed.
        var basePath = $"{_jiraSettings.FrontendBaseUrl.TrimEnd('/')}/settings";
        return success
            ? $"{basePath}?jira=connected"
            : $"{basePath}?jira=error&message={Uri.EscapeDataString(errorMessage ?? "Unknown error")}";
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisconnectJiraCommand(User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await _mediator.Send(new GetJiraProjectsQuery(User.GetUserId()), cancellationToken);
        return Ok(projects);
    }

    [HttpGet("projects/{projectKey}/preview")]
    public async Task<IActionResult> PreviewImport(string projectKey, CancellationToken cancellationToken)
    {
        var rows = await _mediator.Send(new PreviewJiraImportQuery(User.GetUserId(), projectKey), cancellationToken);
        return Ok(rows);
    }

    [HttpPost("projects/{projectKey}/import")]
    public async Task<IActionResult> Import(string projectKey, [FromQuery] string teamId, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new ImportFromJiraCommand(teamId, User.GetUserId(), projectKey), cancellationToken);
        return Ok(summary);
    }

    /// <summary>Creates a brand-new team from a Jira project in one step, instead of importing into an existing one.</summary>
    [HttpPost("projects/{projectKey}/create-team")]
    public async Task<IActionResult> CreateTeamFromProject(string projectKey, [FromBody] CreateTeamFromJiraRequest? request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTeamFromJiraCommand(User.GetUserId(), projectKey, request?.TeamName), cancellationToken);
        return Ok(result);
    }

    public record CreateTeamFromJiraRequest(string? TeamName);
}
