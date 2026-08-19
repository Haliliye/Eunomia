namespace TodoApp.Domain.UserStories;

public interface IUserStoryRepository
{
    Task<UserStory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserStory>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task AddAsync(UserStory story, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserStory story, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only if the story's persisted Version still matches expectedVersion —
    /// returns false (instead of throwing) if someone else saved a change first,
    /// so the caller can decide how to report the conflict.
    /// </summary>
    Task<bool> UpdateWithConcurrencyCheckAsync(UserStory story, int expectedVersion, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Bulk-deletes every story belonging to a team (used when a team is deleted — US-103).</summary>
    Task DeleteByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Server-side filtered, paginated search (US-115/US-116) — pushes status/priority/
    /// assignee filters and keyword search down to MongoDB instead of filtering in memory,
    /// and returns only one page of results plus the total match count.
    /// </summary>
    Task<(IReadOnlyList<UserStory> Items, int TotalCount)> SearchAsync(
        string teamId,
        string? status,
        string? priority,
        string? assigneeId,
        string? keyword,
        int page,
        int pageSize,
        bool showArchived = false,
        string? sprintId = null,
        string? labelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// US-120: candidates for the due-soon reminder check — not archived, not
    /// Done, has a due date and an assignee, and hasn't been reminded for the
    /// current due date yet. The exact per-user lead-time window is applied
    /// in-memory by the caller (DueDateReminderBackgroundService) since it
    /// varies per assignee and isn't worth pushing into the Mongo filter.
    /// </summary>
    Task<IReadOnlyList<UserStory>> GetPendingReminderCandidatesAsync(CancellationToken cancellationToken = default);

    /// <summary>US-142 "My Work": every non-archived story assigned to this user,
    /// across ALL of their teams — assignee ids are globally unique per user, so
    /// this doesn't need to know which teams to look in.</summary>
    Task<IReadOnlyList<UserStory>> GetByAssigneeIdAsync(string assigneeId, CancellationToken cancellationToken = default);

    /// <summary>All subtasks of the given parent story — see UserStory.ParentId. SearchAsync/GetByTeamIdAsync deliberately exclude these (a subtask isn't a normal top-level backlog/board item), so this is the only way to fetch them.</summary>
    Task<IReadOnlyList<UserStory>> GetByParentIdAsync(string parentId, CancellationToken cancellationToken = default);

    /// <summary>Every story in this team that has one of the given Jira issue keys already on file — used to decide create-vs-update when re-importing the same Jira project (see UserStoryRowApplier). One query instead of N.</summary>
    Task<IReadOnlyList<UserStory>> GetByJiraIssueKeysAsync(string teamId, IEnumerable<string> jiraIssueKeys, CancellationToken cancellationToken = default);

    /// <summary>Same idea as GetByJiraIssueKeysAsync but for Azure DevOps work item ids.</summary>
    Task<IReadOnlyList<UserStory>> GetByAzureDevOpsWorkItemIdsAsync(string teamId, IEnumerable<string> workItemIds, CancellationToken cancellationToken = default);
    /// <summary>Same idea as GetByAzureDevOpsWorkItemIdsAsync but for GitHub issue keys ("{owner}/{repo}#{number}").</summary>
    Task<IReadOnlyList<UserStory>> GetByGitHubIssueKeysAsync(string teamId, IEnumerable<string> gitHubIssueKeys, CancellationToken cancellationToken = default);
}
