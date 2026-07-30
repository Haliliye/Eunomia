using MediatR;

namespace TodoApp.Application.UserStories.Commands.AddStoryLink;

/// <summary>LinkType is from the story's OWN perspective — "Blocks" means this story blocks LinkedStoryId; the reverse ("BlockedBy") is created automatically on the other side.</summary>
public record AddStoryLinkCommand(string StoryId, string LinkedStoryId, string LinkType, string RequestingUserId) : IRequest;
