using MediatR;

namespace TodoApp.Application.Integrations.Jira.Commands;

public record DisconnectJiraCommand(string RequestingUserId) : IRequest;
