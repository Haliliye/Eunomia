using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Commands.CreateUserStory;

public record CreateUserStoryCommand(string TeamId, string Title, string? Description, string CreatedByUserId) : IRequest<UserStoryDto>;
