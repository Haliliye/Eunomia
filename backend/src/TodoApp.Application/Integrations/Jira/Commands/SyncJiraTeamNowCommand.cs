using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira.Commands;

/// <summary>Manual "Sync now" button on a Jira-linked team — reuses the team's existing JiraProjectSync record (project key + whose connection to use) so the caller only needs a teamId, not the project key again.</summary>
public record SyncJiraTeamNowCommand(string TeamId, string RequestingUserId) : IRequest<ImportSummaryDto>;
