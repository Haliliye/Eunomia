using MediatR;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public record DisconnectGitHubCommand(string RequestingUserId) : IRequest;
