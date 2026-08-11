using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

/// <summary>Manual "Sync now" button on an Azure DevOps-linked team — reuses the team's existing AzureDevOpsProjectSync record (project name + whose connection to use) so the caller only needs a teamId.</summary>
public record SyncAzureDevOpsTeamNowCommand(string TeamId, string RequestingUserId) : IRequest<ImportSummaryDto>;
