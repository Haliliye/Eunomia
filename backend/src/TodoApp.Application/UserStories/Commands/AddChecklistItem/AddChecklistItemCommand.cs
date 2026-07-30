using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Commands.AddChecklistItem;

public record AddChecklistItemCommand(string UserStoryId, string Text, string RequestingUserId) : IRequest<ChecklistItemDto>;
