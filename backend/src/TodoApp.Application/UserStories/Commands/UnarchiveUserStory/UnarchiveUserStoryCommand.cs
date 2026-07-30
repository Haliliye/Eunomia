using MediatR;

namespace TodoApp.Application.UserStories.Commands.UnarchiveUserStory;

public record UnarchiveUserStoryCommand(string UserStoryId, string RequestingUserId = "") : IRequest;
