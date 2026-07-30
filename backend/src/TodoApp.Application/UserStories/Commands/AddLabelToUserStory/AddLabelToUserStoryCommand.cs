using MediatR;

namespace TodoApp.Application.UserStories.Commands.AddLabelToUserStory;

public record AddLabelToUserStoryCommand(string UserStoryId, string LabelId, string RequestingUserId) : IRequest;
