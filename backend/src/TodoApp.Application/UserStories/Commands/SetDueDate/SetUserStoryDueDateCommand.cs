using MediatR;

namespace TodoApp.Application.UserStories.Commands.SetDueDate;

/// <summary>Narrow, single-field version of the due date part of UpdateUserStoryCommand — lets bulk actions and quick-edit UI set just this field without needing the current title/description/storyPoints/expectedVersion the full update requires.</summary>
public record SetUserStoryDueDateCommand(string UserStoryId, DateTime? DueDate, string RequestingUserId = "") : IRequest;
