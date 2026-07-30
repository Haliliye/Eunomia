namespace TodoApp.Application.UserStories.Queries.GlobalSearch;

public record GlobalSearchResultDto(string StoryId, string Title, string TeamId, string TeamName, string Status);
