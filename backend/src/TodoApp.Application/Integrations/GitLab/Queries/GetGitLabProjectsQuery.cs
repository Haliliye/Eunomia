using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.GitLab.Queries;

public record GetGitLabProjectsQuery(string RequestingUserId) : IRequest<IReadOnlyList<GitLabProjectDto>>;
