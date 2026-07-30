using MediatR;

namespace TodoApp.Application.PersonalTasks.Queries.GetMyWork;

public record GetMyWorkQuery(string UserId) : IRequest<IReadOnlyList<MyWorkItemDto>>;
