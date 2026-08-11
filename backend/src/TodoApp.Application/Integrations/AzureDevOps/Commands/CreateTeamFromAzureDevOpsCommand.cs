using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record CreateTeamFromAzureDevOpsCommand(string RequestingUserId, string ProjectName, string? TeamName, bool? SetAutoSync = null) : IRequest<CreateTeamFromAzureDevOpsResult>;

public record CreateTeamFromAzureDevOpsResult(TeamDto Team, ImportSummaryDto ImportSummary);
