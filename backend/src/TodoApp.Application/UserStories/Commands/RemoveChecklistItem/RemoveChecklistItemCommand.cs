using MediatR;

namespace TodoApp.Application.UserStories.Commands.RemoveChecklistItem;

public record RemoveChecklistItemCommand(string UserStoryId, string ItemId, string RequestingUserId) : IRequest;
