using MediatR;
using TodoApp.Application.PersonalTasks.DTOs;
using TodoApp.Domain.PersonalTasks;

namespace TodoApp.Application.PersonalTasks.Queries.GetMyPersonalTasks;

public class GetMyPersonalTasksQueryHandler : IRequestHandler<GetMyPersonalTasksQuery, IReadOnlyList<PersonalTaskDto>>
{
    private readonly IPersonalTaskRepository _repository;

    public GetMyPersonalTasksQueryHandler(IPersonalTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PersonalTaskDto>> Handle(GetMyPersonalTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _repository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);

        return tasks
            .OrderByDescending(t => t.CreatedOn)
            .Select(t => new PersonalTaskDto(t.Id, t.Title, t.Description, t.DueDate, t.IsCompleted, t.CreatedOn, t.ConvertedToUserStoryId))
            .ToList();
    }
}
