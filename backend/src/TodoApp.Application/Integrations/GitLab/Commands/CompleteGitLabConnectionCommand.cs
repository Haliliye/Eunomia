using MediatR;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public record CompleteGitLabConnectionCommand(string Code, string State) : IRequest<CompleteGitLabConnectionResult>;

public record CompleteGitLabConnectionResult(bool Success, string? GitLabUsername, string? ErrorMessage);
