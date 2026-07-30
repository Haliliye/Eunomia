using MediatR;

namespace TodoApp.Application.UserStories.Queries.GlobalSearch;

/// <summary>Searches across every team the caller is a member of — the command-palette (Ctrl/Cmd+K) search.</summary>
public record GlobalSearchQuery(string RequestingUserId, string Keyword) : IRequest<IReadOnlyList<GlobalSearchResultDto>>;
