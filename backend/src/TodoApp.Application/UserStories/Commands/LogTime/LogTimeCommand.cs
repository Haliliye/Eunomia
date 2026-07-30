using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Commands.LogTime;

public record LogTimeCommand(string UserStoryId, double Hours, string? Note, string RequestingUserId) : IRequest<TimeLogEntryDto>;
