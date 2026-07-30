using MediatR;
using TodoApp.Application.PersonalTasks.DTOs;
using TodoApp.Domain.PersonalTasks;

namespace TodoApp.Application.PersonalTasks.Commands.CreatePersonalTask;

public class CreatePersonalTaskCommandHandler : IRequestHandler<CreatePersonalTaskCommand, PersonalTaskDto>
{
    private readonly IPersonalTaskRepository _repository;

    public CreatePersonalTaskCommandHandler(IPersonalTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<PersonalTaskDto> Handle(CreatePersonalTaskCommand request, CancellationToken cancellationToken)
    {
        var task = PersonalTask.Create(Guid.NewGuid().ToString(), request.OwnerUserId, request.Title, request.Description, request.DueDate);
        await _repository.AddAsync(task, cancellationToken);

        return new PersonalTaskDto(task.Id, task.Title, task.Description, task.DueDate, task.IsCompleted, task.CreatedOn, task.ConvertedToUserStoryId);
    }
}
