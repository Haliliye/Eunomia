using MediatR;

namespace TodoApp.Application.Integrations.GitHub.Queries;

public record GetGitHubStatusQuery(string RequestingUserId) : IRequest<GitHubStatusDto>;

public record GitHubStatusDto(bool IsConnected, string? GitHubLogin, DateTime? ConnectedOn);
