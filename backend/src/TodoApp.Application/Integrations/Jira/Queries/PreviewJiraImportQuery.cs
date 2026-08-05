using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira.Queries;

/// <summary>Fetches + maps a Jira project's issues but creates nothing yet — mirrors PreviewImportUserStoriesCommand's "review before confirming" step for CSV.</summary>
public record PreviewJiraImportQuery(string RequestingUserId, string ProjectKey) : IRequest<IReadOnlyList<ImportRowDto>>;
