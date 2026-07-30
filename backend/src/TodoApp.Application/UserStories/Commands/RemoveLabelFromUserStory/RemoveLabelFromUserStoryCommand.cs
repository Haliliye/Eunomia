using MediatR;

namespace TodoApp.Application.UserStories.Commands.RemoveLabelFromUserStory;

public record RemoveLabelFromUserStoryCommand(string UserStoryId, string LabelId, string RequestingUserId) : IRequest;
