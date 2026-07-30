using MediatR;

namespace TodoApp.Application.UserStories.Commands.SetEstimate;

public record SetEstimateCommand(string UserStoryId, double? Hours, string RequestingUserId) : IRequest;
