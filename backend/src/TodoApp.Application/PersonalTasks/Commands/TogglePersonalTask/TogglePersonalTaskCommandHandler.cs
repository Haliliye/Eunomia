using MediatR;
using TodoApp.Domain.PersonalTasks;

namespace TodoApp.Application.PersonalTasks.Commands.TogglePersonalTask;

public class TogglePersonalTaskCommandHandler : IRequestHandler<TogglePersonalTaskCommand>
{
    private readonly IPersonalTaskRepository _repository;

    public TogglePersonalTaskCommandHandler(IPersonalTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(TogglePersonalTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.OwnerUserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("This isn't your task.");

        task.SetCompleted(request.IsCompleted);
        await _repository.UpdateAsync(task, cancellationToken);
    }
}
