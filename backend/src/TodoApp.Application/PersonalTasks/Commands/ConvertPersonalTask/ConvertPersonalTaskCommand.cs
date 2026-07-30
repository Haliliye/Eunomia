using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.PersonalTasks.Commands.ConvertPersonalTask;

public record ConvertPersonalTaskCommand(string TaskId, string RequestingUserId, string TeamId) : IRequest<UserStoryDto>;
