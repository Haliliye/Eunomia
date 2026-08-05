using MediatR;

namespace TodoApp.Application.Integrations.Jira.Queries;

public record GetJiraStatusQuery(string RequestingUserId) : IRequest<JiraStatusDto>;

public record JiraStatusDto(bool IsConnected, string? SiteName, DateTime? ConnectedOn);
