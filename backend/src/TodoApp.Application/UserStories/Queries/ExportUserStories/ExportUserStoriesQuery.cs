using MediatR;

namespace TodoApp.Application.UserStories.Queries.ExportUserStories;

/// <summary>US-146: same filters as the backlog view, so "export respects active filters" is automatic — the frontend just passes through whatever it's currently filtered by.</summary>
public record ExportUserStoriesQuery(
    string TeamId,
    string RequestingUserId,
    string? Status,
    string? Priority,
    string? AssigneeId,
    string? Keyword,
    string? SprintId,
    string? LabelId,
    bool ShowArchived) : IRequest<string>;
