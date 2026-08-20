using MongoDB.Driver;
using TodoApp.Domain.UserStories;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class UserStoryRepository : IUserStoryRepository
{
    private readonly IMongoCollection<UserStoryDocument> _userStories;

    public UserStoryRepository(MongoDbContext context)
    {
        _userStories = context.UserStories;
    }

    public async Task<UserStory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _userStories.Find(s => s.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<UserStory>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var documents = await _userStories.Find(s => s.TeamId == teamId).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(UserStory story, CancellationToken cancellationToken = default) =>
        await _userStories.InsertOneAsync(ToDocument(story), cancellationToken: cancellationToken);

    public async Task UpdateAsync(UserStory story, CancellationToken cancellationToken = default) =>
        await _userStories.ReplaceOneAsync(s => s.Id == story.Id, ToDocument(story), cancellationToken: cancellationToken);

    /// <summary>
    /// Optimistic-concurrency-checked update for US-107 (concurrent edits).
    /// The filter matches on Id AND the version the caller last saw — if
    /// someone else saved a change in between, MatchedCount is 0 and this
    /// returns false instead of silently overwriting their edit.
    /// </summary>
    public async Task<bool> UpdateWithConcurrencyCheckAsync(UserStory story, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.Id, story.Id),
            Builders<UserStoryDocument>.Filter.Eq(s => s.Version, expectedVersion));

        var result = await _userStories.ReplaceOneAsync(filter, ToDocument(story), cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _userStories.DeleteOneAsync(s => s.Id == id, cancellationToken);

    public async Task DeleteByTeamIdAsync(string teamId, CancellationToken cancellationToken = default) =>
        await _userStories.DeleteManyAsync(s => s.TeamId == teamId, cancellationToken);

    public async Task<(IReadOnlyList<UserStory> Items, int TotalCount)> SearchAsync(
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
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<UserStoryDocument>>
        {
            Builders<UserStoryDocument>.Filter.Eq(s => s.TeamId, teamId),
            // Archived stories are hidden from the normal backlog/board/dashboard
            // views by default — the Archived tab explicitly asks for the opposite.
            Builders<UserStoryDocument>.Filter.Eq(s => s.IsArchived, showArchived),
            // Subtasks (ParentId set) aren't independent backlog/board items —
            // they only ever appear nested under their parent's detail page
            // (see GetByParentIdAsync), never in this top-level list.
            Builders<UserStoryDocument>.Filter.Eq(s => s.ParentId, null),
        };

        if (!string.IsNullOrWhiteSpace(status))
            filters.Add(Builders<UserStoryDocument>.Filter.Eq(s => s.Status, status));

        if (!string.IsNullOrWhiteSpace(priority))
            filters.Add(Builders<UserStoryDocument>.Filter.Eq(s => s.Priority, priority));

        if (!string.IsNullOrWhiteSpace(assigneeId))
            filters.Add(Builders<UserStoryDocument>.Filter.Eq(s => s.AssigneeId, assigneeId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // Uses the text index created in MongoIndexInitializer over Title+Description.
            // NOTE: this is whole-word/stemmed matching, not substring "contains" — e.g.
            // searching "log" will no longer match "login" the way the old in-memory
            // Contains() did. That's the tradeoff for pushing search to an index so it
            // scales; see README for details.
            filters.Add(Builders<UserStoryDocument>.Filter.Text(keyword));
        }

        // sprintId: null = don't filter by sprint at all; "none" = only stories
        // still in the backlog (no sprint yet); anything else = that specific sprint.
        if (sprintId == "none")
            filters.Add(Builders<UserStoryDocument>.Filter.Eq(s => s.SprintId, null));
        else if (!string.IsNullOrWhiteSpace(sprintId))
            filters.Add(Builders<UserStoryDocument>.Filter.Eq(s => s.SprintId, sprintId));

        if (!string.IsNullOrWhiteSpace(labelId))
            filters.Add(Builders<UserStoryDocument>.Filter.AnyEq(s => s.LabelIds, labelId));

        var filter = Builders<UserStoryDocument>.Filter.And(filters);

        var totalCount = (int)await _userStories.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var documents = await _userStories.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (documents.Select(ToDomain).ToList(), totalCount);
    }

    private static UserStoryDocument ToDocument(UserStory story) => new()
    {
        Id = story.Id,
        TeamId = story.TeamId,
        Title = story.Title,
        Description = story.Description,
        Status = story.Status.ToString(),
        Priority = story.Priority.ToString(),
        AssigneeId = story.AssigneeId,
        DueDate = story.DueDate,
        CreatedOn = story.CreatedOn,
        Version = story.Version,
        IsArchived = story.IsArchived,
        StoryPoints = story.StoryPoints,
        SprintId = story.SprintId,
        ReminderSentOn = story.ReminderSentOn,
        RecurrenceFrequency = story.RecurrenceFrequency?.ToString(),
        RecurrenceEndDate = story.RecurrenceEndDate,
        LabelIds = story.LabelIds.ToList(),
        ChecklistItems = story.ChecklistItems
            .Select(i => new ChecklistItemDocument { Id = i.Id, Text = i.Text, IsCompleted = i.IsCompleted, Order = i.Order })
            .ToList(),
        Attachments = story.Attachments
            .Select(a => new AttachmentDocument
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                StorageKey = a.StorageKey,
                UploadedByUserId = a.UploadedByUserId,
                UploadedOn = a.UploadedOn
            })
            .ToList(),
        EstimatedHours = story.EstimatedHours,
        TimeLogEntries = story.TimeLogEntries
            .Select(t => new TimeLogEntryDocument { Id = t.Id, Hours = t.Hours, Note = t.Note, LoggedByUserId = t.LoggedByUserId, LoggedOn = t.LoggedOn })
            .ToList(),
        Links = story.Links
            .Select(l => new StoryLinkDocument { LinkedStoryId = l.LinkedStoryId, LinkType = l.LinkType.ToString() })
            .ToList(),
        CreatedByUserId = story.CreatedByUserId,
        ParentId = story.ParentId,
        JiraIssueKey = story.JiraIssueKey,
        EpicId = story.EpicId,
        AzureDevOpsWorkItemId = story.AzureDevOpsWorkItemId,
        GitHubIssueKey = story.GitHubIssueKey,
        GitLabIssueKey = story.GitLabIssueKey
    };

    private static UserStory ToDomain(UserStoryDocument document) => UserStory.Rehydrate(
        document.Id,
        document.TeamId,
        document.Title,
        document.Description,
        document.Status,
        Enum.Parse<UserStoryPriority>(document.Priority),
        document.AssigneeId,
        document.DueDate,
        document.CreatedOn,
        document.Version,
        document.IsArchived,
        document.StoryPoints,
        document.SprintId,
        document.ChecklistItems.Select(i => ChecklistItem.Rehydrate(i.Id, i.Text, i.IsCompleted, i.Order)),
        document.ReminderSentOn,
        document.LabelIds,
        document.RecurrenceFrequency is null ? null : Enum.Parse<RecurrenceFrequency>(document.RecurrenceFrequency),
        document.RecurrenceEndDate,
        document.Attachments.Select(a => new Attachment(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.StorageKey, a.UploadedByUserId, a.UploadedOn)),
        document.EstimatedHours,
        document.TimeLogEntries.Select(t => new TimeLogEntry(t.Id, t.Hours, t.Note, t.LoggedByUserId, t.LoggedOn)),
        document.Links.Select(l => new StoryLink(l.LinkedStoryId, Enum.Parse<StoryLinkType>(l.LinkType))),
        document.CreatedByUserId,
        document.ParentId,
        document.JiraIssueKey,
        document.EpicId,
        document.AzureDevOpsWorkItemId,
        document.GitHubIssueKey,
        document.GitLabIssueKey);

    public async Task<IReadOnlyList<UserStory>> GetPendingReminderCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.IsArchived, false),
            Builders<UserStoryDocument>.Filter.Ne(s => s.Status, "Done"),
            Builders<UserStoryDocument>.Filter.Ne(s => s.DueDate, null),
            Builders<UserStoryDocument>.Filter.Ne(s => s.AssigneeId, null),
            Builders<UserStoryDocument>.Filter.Eq(s => s.ReminderSentOn, null));

        var documents = await _userStories.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UserStory>> GetByAssigneeIdAsync(string assigneeId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.AssigneeId, assigneeId),
            Builders<UserStoryDocument>.Filter.Eq(s => s.IsArchived, false));

        var documents = await _userStories.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UserStory>> GetByParentIdAsync(string parentId, CancellationToken cancellationToken = default)
    {
        var documents = await _userStories.Find(s => s.ParentId == parentId).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UserStory>> GetByJiraIssueKeysAsync(string teamId, IEnumerable<string> jiraIssueKeys, CancellationToken cancellationToken = default)
    {
        var keys = jiraIssueKeys.ToList();
        if (keys.Count == 0) return new List<UserStory>();

        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.TeamId, teamId),
            Builders<UserStoryDocument>.Filter.In(s => s.JiraIssueKey, keys));

        var documents = await _userStories.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UserStory>> GetByAzureDevOpsWorkItemIdsAsync(string teamId, IEnumerable<string> workItemIds, CancellationToken cancellationToken = default)
    {
        var ids = workItemIds.ToList();
        if (ids.Count == 0) return new List<UserStory>();

        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.TeamId, teamId),
            Builders<UserStoryDocument>.Filter.In(s => s.AzureDevOpsWorkItemId, ids));

        var documents = await _userStories.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UserStory>> GetByGitHubIssueKeysAsync(string teamId, IEnumerable<string> gitHubIssueKeys, CancellationToken cancellationToken = default)
    {
        var keys = gitHubIssueKeys.ToList();
        if (keys.Count == 0) return new List<UserStory>();

        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.TeamId, teamId),
            Builders<UserStoryDocument>.Filter.In(s => s.GitHubIssueKey, keys));

        var documents = await _userStories.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UserStory>> GetByGitLabIssueKeysAsync(string teamId, IEnumerable<string> gitLabIssueKeys, CancellationToken cancellationToken = default)
    {
        var keys = gitLabIssueKeys.ToList();
        if (keys.Count == 0) return new List<UserStory>();

        var filter = Builders<UserStoryDocument>.Filter.And(
            Builders<UserStoryDocument>.Filter.Eq(s => s.TeamId, teamId),
            Builders<UserStoryDocument>.Filter.In(s => s.GitLabIssueKey, keys));

        var documents = await _userStories.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }
}