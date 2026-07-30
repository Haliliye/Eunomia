namespace TodoApp.Domain.Comments;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comment>> GetByUserStoryIdAsync(string userStoryId, CancellationToken cancellationToken = default);
    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Bulk-deletes every comment on the given stories (used in cascade deletes — US-103, US-108).</summary>
    Task DeleteByUserStoryIdsAsync(IEnumerable<string> userStoryIds, CancellationToken cancellationToken = default);
}
