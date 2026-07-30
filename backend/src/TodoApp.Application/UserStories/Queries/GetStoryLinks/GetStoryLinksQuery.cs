using MediatR;

namespace TodoApp.Application.UserStories.Queries.GetStoryLinks;

public record GetStoryLinksQuery(string StoryId, string RequestingUserId) : IRequest<IReadOnlyList<ResolvedStoryLinkDto>>;
