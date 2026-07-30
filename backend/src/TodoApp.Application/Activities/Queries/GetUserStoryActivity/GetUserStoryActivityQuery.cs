using MediatR;
using TodoApp.Application.Activities.DTOs;

namespace TodoApp.Application.Activities.Queries.GetUserStoryActivity;

public record GetUserStoryActivityQuery(string UserStoryId, string RequestingUserId, int Limit = 50) : IRequest<IReadOnlyList<ActivityDto>>;
