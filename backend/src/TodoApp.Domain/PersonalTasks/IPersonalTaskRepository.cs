namespace TodoApp.Domain.PersonalTasks;

public interface IPersonalTaskRepository
{
    Task<PersonalTask?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonalTask>> GetByOwnerIdAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(PersonalTask task, CancellationToken cancellationToken = default);
    Task UpdateAsync(PersonalTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
