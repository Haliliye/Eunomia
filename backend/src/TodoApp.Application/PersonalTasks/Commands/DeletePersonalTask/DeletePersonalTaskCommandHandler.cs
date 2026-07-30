using MediatR;
using TodoApp.Domain.PersonalTasks;

namespace TodoApp.Application.PersonalTasks.Commands.DeletePersonalTask;

public class DeletePersonalTaskCommandHandler : IRequestHandler<DeletePersonalTaskCommand>
{
    private readonly IPersonalTaskRepository _repository;

    public DeletePersonalTaskCommandHandler(IPersonalTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeletePersonalTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.OwnerUserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("This isn't your task.");

        await _repository.DeleteAsync(request.TaskId, cancellationToken);
    }
}
