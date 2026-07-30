using MediatR;

namespace TodoApp.Application.UserStories.Commands.RemoveStoryLink;

public record RemoveStoryLinkCommand(string StoryId, string LinkedStoryId, string RequestingUserId) : IRequest;
