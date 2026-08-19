using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Common;
using TodoApp.Application.Integrations.GitHub.Commands;
using TodoApp.Application.Integrations.GitHub.Queries;
using TodoApp.Infrastructure.Integrations.GitHub;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/integrations/github")]
public class GitHubIntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly GitHubSettings _gitHubSettings;

    public GitHubIntegrationsController(IMediator mediator, IOptions<GitHubSettings> gitHubSettings)
    {
        _mediator = mediator;
        _gitHubSettings = gitHubSettings.Value;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetGitHubStatusQuery(User.GetUserId()), cancellationToken);
        return Ok(status);
    }

    /// <summary>Returns the URL for the frontend to do a full-page redirect to — GitHub's consent screen can't run inside an XHR or an iframe.</summary>
    [HttpGet("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var authorizationUrl = await _mediator.Send(new StartGitHubConnectionCommand(User.GetUserId()), cancellationToken);
        return Ok(new { authorizationUrl });
    }

    /// <summary>
    /// GitHub redirects the user's browser here directly — no JWT is sent,
    /// so this must be anonymous. The user's identity instead comes from
    /// the encrypted "state" value we handed GitHub in Connect(). Always
    /// ends in a redirect back to the frontend, success or failure.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect(BuildFrontendRedirect(success: false, "The GitHub authorization was cancelled or denied."));

        var result = await _mediator.Send(new CompleteGitHubConnectionCommand(code, state), cancellationToken);
        return Redirect(BuildFrontendRedirect(result.Success, result.ErrorMessage));
    }

    private string BuildFrontendRedirect(bool success, string? errorMessage)
    {
        var basePath = $"{_gitHubSettings.FrontendBaseUrl.TrimEnd('/')}/settings";
        return success
            ? $"{basePath}?github=connected"
            : $"{basePath}?github=error&message={Uri.EscapeDataString(errorMessage ?? "Unknown error")}";
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisconnectGitHubCommand(User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("repositories")]
    public async Task<IActionResult> GetRepositories(CancellationToken cancellationToken)
    {
        var repositories = await _mediator.Send(new GetGitHubRepositoriesQuery(User.GetUserId()), cancellationToken);
        return Ok(repositories);
    }

    [HttpPost("repos/{owner}/{repo}/import")]
    public async Task<IActionResult> Import(string owner, string repo, [FromQuery] string teamId, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new ImportFromGitHubCommand(teamId, User.GetUserId(), owner, repo), cancellationToken);
        return Ok(summary);
    }

    /// <summary>Creates a brand-new team from a GitHub repo in one step, instead of importing into an existing one.</summary>
    [HttpPost("repos/{owner}/{repo}/create-team")]
    public async Task<IActionResult> CreateTeamFromRepo(string owner, string repo, [FromBody] CreateTeamFromGitHubRequest? request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTeamFromGitHubCommand(User.GetUserId(), owner, repo, request?.TeamName), cancellationToken);
        return Ok(result);
    }

    public record CreateTeamFromGitHubRequest(string? TeamName);
}
