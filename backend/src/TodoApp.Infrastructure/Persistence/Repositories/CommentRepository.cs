using MongoDB.Driver;
using TodoApp.Domain.Comments;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly IMongoCollection<CommentDocument> _comments;

    public CommentRepository(MongoDbContext context)
    {
        _comments = context.Comments;
    }

    public async Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _comments.Find(c => c.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<Comment>> GetByUserStoryIdAsync(string userStoryId, CancellationToken cancellationToken = default)
    {
        var documents = await _comments.Find(c => c.UserStoryId == userStoryId).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default) =>
        await _comments.InsertOneAsync(ToDocument(comment), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default) =>
        await _comments.ReplaceOneAsync(c => c.Id == comment.Id, ToDocument(comment), cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _comments.DeleteOneAsync(c => c.Id == id, cancellationToken);

    public async Task DeleteByUserStoryIdsAsync(IEnumerable<string> userStoryIds, CancellationToken cancellationToken = default)
    {
        var ids = userStoryIds.ToList();
        if (ids.Count == 0) return;

        var filter = Builders<CommentDocument>.Filter.In(c => c.UserStoryId, ids);
        await _comments.DeleteManyAsync(filter, cancellationToken);
    }

    private static CommentDocument ToDocument(Comment comment) => new()
    {
        Id = comment.Id,
        UserStoryId = comment.UserStoryId,
        AuthorId = comment.AuthorId,
        Content = comment.Content,
        MentionedUserIds = comment.MentionedUserIds.ToList(),
        CreatedOn = comment.CreatedOn,
        EditedOn = comment.EditedOn
    };

    private static Comment ToDomain(CommentDocument document) => Comment.Rehydrate(
        document.Id, document.UserStoryId, document.AuthorId, document.Content,
        document.MentionedUserIds, document.CreatedOn, document.EditedOn);
}
