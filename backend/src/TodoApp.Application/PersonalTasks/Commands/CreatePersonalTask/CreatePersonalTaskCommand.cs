using MediatR;
using TodoApp.Application.PersonalTasks.DTOs;

namespace TodoApp.Application.PersonalTasks.Commands.CreatePersonalTask;

public record CreatePersonalTaskCommand(string OwnerUserId, string Title, string? Description, DateTime? DueDate) : IRequest<PersonalTaskDto>;
