using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.GitHub.Queries;

public record GetGitHubRepositoriesQuery(string RequestingUserId) : IRequest<IReadOnlyList<GitHubRepositoryDto>>;
