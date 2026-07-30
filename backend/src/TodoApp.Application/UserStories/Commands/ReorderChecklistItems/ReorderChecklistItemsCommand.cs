using MediatR;

namespace TodoApp.Application.UserStories.Commands.ReorderChecklistItems;

public record ReorderChecklistItemsCommand(string UserStoryId, IReadOnlyList<string> OrderedItemIds, string RequestingUserId) : IRequest;
