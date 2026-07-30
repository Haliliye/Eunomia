using MediatR;
using TodoApp.Domain.PersonalTasks;

namespace TodoApp.Application.PersonalTasks.Commands.UpdatePersonalTask;

public class UpdatePersonalTaskCommandHandler : IRequestHandler<UpdatePersonalTaskCommand>
{
    private readonly IPersonalTaskRepository _repository;

    public UpdatePersonalTaskCommandHandler(IPersonalTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdatePersonalTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        // A personal task is only ever visible to (and thus only ever
        // editable by) its owner — there's no team/sharing concept here.
        if (task.OwnerUserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("This isn't your task.");

        task.Update(request.Title, request.Description, request.DueDate);
        await _repository.UpdateAsync(task, cancellationToken);
    }
}
