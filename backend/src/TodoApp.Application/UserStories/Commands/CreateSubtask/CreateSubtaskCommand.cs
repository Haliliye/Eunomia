using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Commands.CreateSubtask;

public record CreateSubtaskCommand(string ParentStoryId, string Title, string RequestingUserId) : IRequest<UserStoryDto>;
