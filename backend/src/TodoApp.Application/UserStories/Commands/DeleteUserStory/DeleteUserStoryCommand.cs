using MediatR;

namespace TodoApp.Application.UserStories.Commands.DeleteUserStory;

public record DeleteUserStoryCommand(string UserStoryId, string RequestingUserId) : IRequest;
