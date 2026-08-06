using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira.Commands;

/// <summary>Creates a brand-new team from a Jira project in one step — "TeamName" left null defaults to the Jira project's own name.</summary>
public record CreateTeamFromJiraCommand(string RequestingUserId, string ProjectKey, string? TeamName, bool? SetAutoSync = null) : IRequest<CreateTeamFromJiraResult>;

public record CreateTeamFromJiraResult(TeamDto Team, ImportSummaryDto ImportSummary);
