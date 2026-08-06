using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Queries.GetSubtasks;

public record GetSubtasksQuery(string ParentStoryId, string RequestingUserId) : IRequest<IReadOnlyList<UserStoryDto>>;
