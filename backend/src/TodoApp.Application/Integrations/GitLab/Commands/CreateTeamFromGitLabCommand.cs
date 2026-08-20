using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.GitLab.Commands;

/// <summary>Creates a brand-new team from a GitLab project's issues in one step — "TeamName" left null defaults to the project's own name.</summary>
public record CreateTeamFromGitLabCommand(string RequestingUserId, int ProjectId, string PathWithNamespace, string ProjectName, string? TeamName) : IRequest<CreateTeamFromGitLabResult>;

public record CreateTeamFromGitLabResult(TeamDto Team, ImportSummaryDto ImportSummary);
