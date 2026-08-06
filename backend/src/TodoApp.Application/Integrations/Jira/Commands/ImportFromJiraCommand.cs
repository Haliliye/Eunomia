using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira.Commands;

public record ImportFromJiraCommand(string TeamId, string RequestingUserId, string ProjectKey, bool? SetAutoSync = null) : IRequest<ImportSummaryDto>;
