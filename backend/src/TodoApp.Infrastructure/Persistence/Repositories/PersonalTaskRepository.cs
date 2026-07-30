using MongoDB.Driver;
using TodoApp.Domain.PersonalTasks;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class PersonalTaskRepository : IPersonalTaskRepository
{
    private readonly IMongoCollection<PersonalTaskDocument> _tasks;

    public PersonalTaskRepository(MongoDbContext context)
    {
        _tasks = context.PersonalTasks;
    }

    public async Task<PersonalTask?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<PersonalTask>> GetByOwnerIdAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var documents = await _tasks.Find(t => t.OwnerUserId == ownerUserId).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(PersonalTask task, CancellationToken cancellationToken = default) =>
        await _tasks.InsertOneAsync(ToDocument(task), cancellationToken: cancellationToken);

    public async Task UpdateAsync(PersonalTask task, CancellationToken cancellationToken = default) =>
        await _tasks.ReplaceOneAsync(t => t.Id == task.Id, ToDocument(task), cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _tasks.DeleteOneAsync(t => t.Id == id, cancellationToken);

    private static PersonalTaskDocument ToDocument(PersonalTask task) => new()
    {
        Id = task.Id,
        OwnerUserId = task.OwnerUserId,
        Title = task.Title,
        Description = task.Description,
        DueDate = task.DueDate,
        IsCompleted = task.IsCompleted,
        CreatedOn = task.CreatedOn,
        ConvertedToUserStoryId = task.ConvertedToUserStoryId
    };

    private static PersonalTask ToDomain(PersonalTaskDocument document) => PersonalTask.Rehydrate(
        document.Id, document.OwnerUserId, document.Title, document.Description, document.DueDate,
        document.IsCompleted, document.CreatedOn, document.ConvertedToUserStoryId);
}
