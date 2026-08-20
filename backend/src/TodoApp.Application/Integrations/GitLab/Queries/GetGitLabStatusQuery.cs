using MediatR;

namespace TodoApp.Application.Integrations.GitLab.Queries;

public record GetGitLabStatusQuery(string RequestingUserId) : IRequest<GitLabStatusDto>;

public record GitLabStatusDto(bool IsConnected, string? GitLabUsername, DateTime? ConnectedOn);
