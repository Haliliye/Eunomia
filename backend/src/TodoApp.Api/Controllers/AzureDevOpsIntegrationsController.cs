using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.Integrations.AzureDevOps.Commands;
using TodoApp.Application.Integrations.AzureDevOps.Queries;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/integrations/azuredevops")]
public class AzureDevOpsIntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AzureDevOpsIntegrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetAzureDevOpsStatusQuery(User.GetUserId()), cancellationToken);
        return Ok(status);
    }

    /// <summary>PAT-based (see AzureDevOpsConnection) — no OAuth redirect, just an organization name and a token pasted in from Azure DevOps' own "Personal Access Tokens" settings page.</summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConnectAzureDevOpsCommand(User.GetUserId(), request.OrganizationName, request.PersonalAccessToken), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    public record ConnectRequest(string OrganizationName, string PersonalAccessToken);

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisconnectAzureDevOpsCommand(User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await _mediator.Send(new GetAzureDevOpsProjectsQuery(User.GetUserId()), cancellationToken);
        return Ok(projects);
    }

    [HttpPost("projects/{projectName}/import")]
    public async Task<IActionResult> Import(string projectName, [FromQuery] string teamId, [FromQuery] bool? setAutoSync, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new ImportFromAzureDevOpsCommand(teamId, User.GetUserId(), projectName, setAutoSync), cancellationToken);
        return Ok(summary);
    }

    [HttpPost("projects/{projectName}/create-team")]
    public async Task<IActionResult> CreateTeamFromProject(string projectName, [FromBody] CreateTeamFromAzureDevOpsRequest? request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTeamFromAzureDevOpsCommand(User.GetUserId(), projectName, request?.TeamName, request?.SetAutoSync), cancellationToken);
        return Ok(result);
    }

    public record CreateTeamFromAzureDevOpsRequest(string? TeamName, bool? SetAutoSync);

    [HttpGet("teams/{teamId}/sync-status")]
    public async Task<IActionResult> GetSyncStatus(string teamId, CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetAzureDevOpsSyncStatusQuery(teamId, User.GetUserId()), cancellationToken);
        return Ok(status);
    }

    [HttpPut("teams/{teamId}/auto-sync")]
    public async Task<IActionResult> SetAutoSync(string teamId, [FromBody] SetAutoSyncRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetAzureDevOpsAutoSyncCommand(teamId, User.GetUserId(), request.Enabled), cancellationToken);
        return NoContent();
    }

    public record SetAutoSyncRequest(bool Enabled);

    [HttpPost("teams/{teamId}/sync-now")]
    public async Task<IActionResult> SyncNow(string teamId, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new SyncAzureDevOpsTeamNowCommand(teamId, User.GetUserId()), cancellationToken);
        return Ok(summary);
    }
}
