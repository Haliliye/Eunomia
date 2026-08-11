using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Common;
using TodoApp.Application.Integrations.AzureDevOps.Commands;
using TodoApp.Application.Integrations.AzureDevOps.Queries;
using TodoApp.Infrastructure.Integrations.AzureDevOps;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/integrations/azuredevops")]
public class AzureDevOpsIntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AzureDevOpsSettings _settings;

    public AzureDevOpsIntegrationsController(IMediator mediator, IOptions<AzureDevOpsSettings> settings)
    {
        _mediator = mediator;
        _settings = settings.Value;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetAzureDevOpsStatusQuery(User.GetUserId()), cancellationToken);
        return Ok(status);
    }

    /// <summary>Returns the URL for the frontend to do a full-page redirect to — the Microsoft sign-in/consent screen can't run inside an XHR or an iframe.</summary>
    [HttpGet("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var authorizationUrl = await _mediator.Send(new StartAzureDevOpsConnectionCommand(User.GetUserId()), cancellationToken);
        return Ok(new { authorizationUrl });
    }

    /// <summary>
    /// Microsoft redirects the user's browser here directly — no JWT is sent
    /// (a plain top-level navigation, not our SPA's authenticated fetch), so
    /// this must be anonymous. Identity instead comes from the encrypted
    /// "state" value handed to Microsoft in Connect(). Always ends in a
    /// redirect back to the frontend, success or failure.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect(BuildFrontendRedirect(success: false, "The Azure DevOps authorization was cancelled or denied."));

        var result = await _mediator.Send(new CompleteAzureDevOpsConnectionCommand(code, state), cancellationToken);
        return Redirect(BuildFrontendRedirect(result.Success, result.ErrorMessage));
    }

    private string BuildFrontendRedirect(bool success, string? errorMessage)
    {
        var basePath = $"{_settings.FrontendBaseUrl.TrimEnd('/')}/settings";
        return success
            ? $"{basePath}?azuredevops=connected"
            : $"{basePath}?azuredevops=error&message={Uri.EscapeDataString(errorMessage ?? "Unknown error")}";
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisconnectAzureDevOpsCommand(User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken)
    {
        var organizations = await _mediator.Send(new GetAzureDevOpsOrganizationsQuery(User.GetUserId()), cancellationToken);
        return Ok(organizations);
    }

    [HttpPut("organization")]
    public async Task<IActionResult> SetOrganization([FromBody] SetOrganizationRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetAzureDevOpsOrganizationCommand(User.GetUserId(), request.OrganizationName), cancellationToken);
        return NoContent();
    }

    public record SetOrganizationRequest(string OrganizationName);

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await _mediator.Send(new GetAzureDevOpsProjectsQuery(User.GetUserId()), cancellationToken);
        return Ok(projects);
    }

    [HttpPost("projects/{projectName}/import")]
    public async Task<IActionResult> Import(string projectName, [FromQuery] string teamId, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new ImportFromAzureDevOpsCommand(teamId, User.GetUserId(), projectName), cancellationToken);
        return Ok(summary);
    }

    [HttpPost("projects/{projectName}/create-team")]
    public async Task<IActionResult> CreateTeamFromProject(string projectName, [FromBody] CreateTeamFromAzureDevOpsRequest? request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTeamFromAzureDevOpsCommand(User.GetUserId(), projectName, request?.TeamName), cancellationToken);
        return Ok(result);
    }

    public record CreateTeamFromAzureDevOpsRequest(string? TeamName);
}
