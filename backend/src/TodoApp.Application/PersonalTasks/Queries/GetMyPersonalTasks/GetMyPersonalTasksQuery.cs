using MediatR;
using TodoApp.Application.PersonalTasks.DTOs;

namespace TodoApp.Application.PersonalTasks.Queries.GetMyPersonalTasks;

public record GetMyPersonalTasksQuery(string OwnerUserId) : IRequest<IReadOnlyList<PersonalTaskDto>>;
