using MediatR;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public record DisconnectGitLabCommand(string RequestingUserId) : IRequest;
