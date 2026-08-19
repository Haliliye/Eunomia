using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.GitHub.Commands;

/// <summary>Creates a brand-new team from a GitHub repo's issues in one step — "TeamName" left null defaults to the repo's own name.</summary>
public record CreateTeamFromGitHubCommand(string RequestingUserId, string Owner, string Repo, string? TeamName) : IRequest<CreateTeamFromGitHubResult>;

public record CreateTeamFromGitHubResult(TeamDto Team, ImportSummaryDto ImportSummary);
