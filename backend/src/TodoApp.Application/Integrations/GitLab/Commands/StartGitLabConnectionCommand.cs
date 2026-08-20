using MediatR;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public record StartGitLabConnectionCommand(string RequestingUserId) : IRequest<string>;
