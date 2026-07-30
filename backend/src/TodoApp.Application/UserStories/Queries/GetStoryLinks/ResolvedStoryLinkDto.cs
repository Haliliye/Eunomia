namespace TodoApp.Application.UserStories.Queries.GetStoryLinks;

public record ResolvedStoryLinkDto(string LinkedStoryId, string LinkedStoryTitle, string LinkedStoryTeamId, string LinkType, bool LinkedStoryIsDone);
