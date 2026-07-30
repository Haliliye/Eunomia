using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Queries.GetUserStoryById;

public record GetUserStoryByIdQuery(string UserStoryId, string RequestingUserId) : IRequest<UserStoryDto?>;
