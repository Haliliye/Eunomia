using MediatR;

namespace TodoApp.Application.Integrations.Jira.Commands;

public record SetJiraAutoSyncCommand(string TeamId, string RequestingUserId, bool Enabled) : IRequest;
