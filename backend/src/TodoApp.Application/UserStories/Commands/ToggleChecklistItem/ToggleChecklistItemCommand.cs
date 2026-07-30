using MediatR;

namespace TodoApp.Application.UserStories.Commands.ToggleChecklistItem;

public record ToggleChecklistItemCommand(string UserStoryId, string ItemId, string RequestingUserId) : IRequest;
