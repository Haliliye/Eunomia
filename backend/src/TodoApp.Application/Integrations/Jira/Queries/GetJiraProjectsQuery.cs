using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.Jira.Queries;

public record GetJiraProjectsQuery(string RequestingUserId) : IRequest<IReadOnlyList<JiraProjectDto>>;
