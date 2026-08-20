using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Common;
using TodoApp.Application.Integrations.GitLab.Commands;
using TodoApp.Application.Integrations.GitLab.Queries;
using TodoApp.Infrastructure.Integrations.GitLab;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/integrations/gitlab")]
public class GitLabIntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly GitLabSettings _gitLabSettings;

    public GitLabIntegrationsController(IMediator mediator, IOptions<GitLabSettings> gitLabSettings)
    {
        _mediator = mediator;
        _gitLabSettings = gitLabSettings.Value;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetGitLabStatusQuery(User.GetUserId()), cancellationToken);
        return Ok(status);
    }

    /// <summary>Returns the URL for the frontend to do a full-page redirect to — GitLab's consent screen can't run inside an XHR or an iframe.</summary>
    [HttpGet("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var authorizationUrl = await _mediator.Send(new StartGitLabConnectionCommand(User.GetUserId()), cancellationToken);
        return Ok(new { authorizationUrl });
    }

    /// <summary>
    /// GitLab redirects the user's browser here directly — no JWT is sent,
    /// so this must be anonymous. The user's identity instead comes from
    /// the encrypted "state" value we handed GitLab in Connect(). Always
    /// ends in a redirect back to the frontend, success or failure.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect(BuildFrontendRedirect(success: false, "The GitLab authorization was cancelled or denied."));

        var result = await _mediator.Send(new CompleteGitLabConnectionCommand(code, state), cancellationToken);
        return Redirect(BuildFrontendRedirect(result.Success, result.ErrorMessage));
    }

    private string BuildFrontendRedirect(bool success, string? errorMessage)
    {
        var basePath = $"{_gitLabSettings.FrontendBaseUrl.TrimEnd('/')}/settings";
        return success
            ? $"{basePath}?gitlab=connected"
            : $"{basePath}?gitlab=error&message={Uri.EscapeDataString(errorMessage ?? "Unknown error")}";
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisconnectGitLabCommand(User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await _mediator.Send(new GetGitLabProjectsQuery(User.GetUserId()), cancellationToken);
        return Ok(projects);
    }

    /// <summary>pathWithNamespace is passed by the frontend from the same GetProjects response the person picked this project from — not re-fetched — same reasoning GitHub's controller has for taking owner/repo directly.</summary>
    [HttpPost("projects/{projectId:int}/import")]
    public async Task<IActionResult> Import(int projectId, [FromQuery] string teamId, [FromQuery] string pathWithNamespace, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new ImportFromGitLabCommand(teamId, User.GetUserId(), projectId, pathWithNamespace), cancellationToken);
        return Ok(summary);
    }

    /// <summary>Creates a brand-new team from a GitLab project in one step, instead of importing into an existing one.</summary>
    [HttpPost("projects/{projectId:int}/create-team")]
    public async Task<IActionResult> CreateTeamFromProject(int projectId, [FromBody] CreateTeamFromGitLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTeamFromGitLabCommand(User.GetUserId(), projectId, request.PathWithNamespace, request.ProjectName, request.TeamName), cancellationToken);
        return Ok(result);
    }

    public record CreateTeamFromGitLabRequest(string PathWithNamespace, string ProjectName, string? TeamName);
}
