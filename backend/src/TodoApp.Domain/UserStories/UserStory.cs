using TodoApp.Domain.Common;

namespace TodoApp.Domain.UserStories;

/// <summary>
/// Aggregate root for a single work item (EPIC-2 / EPIC-3). Comments live in
/// their own collection/aggregate (see EPIC-4) referencing this Id, since a
/// story's comment count can grow unbounded and doesn't need to be loaded
/// every time the story itself is loaded.
/// </summary>
public class UserStory : AggregateRoot
{
    public string TeamId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public UserStoryStatus Status { get; private set; } = UserStoryStatus.ToDo;
    public UserStoryPriority Priority { get; private set; } = UserStoryPriority.Medium;
    public string? AssigneeId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedOn { get; private set; }

    /// <summary>
    /// Bumped on every content edit (UpdateDetails). Used for optimistic
    /// concurrency on US-107's "concurrent edits shouldn't silently overwrite
    /// each other" — see UpdateUserStoryCommandHandler and
    /// UserStoryRepository.UpdateWithConcurrencyCheckAsync. Status/priority/
    /// assignee changes deliberately don't bump this: those are single-field,
    /// low-conflict-risk mutations (e.g. dragging a board card) where
    /// last-write-wins is the better UX than a conflict error.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Archived stories are hidden from the Backlog/Board/Dashboard by
    /// default without being deleted — a softer alternative to DeleteUserStory
    /// for "this is done with, but I don't want to lose the history."
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>Optional planning estimate — null means "not estimated yet".</summary>
    public int? StoryPoints { get; private set; }

    /// <summary>Null means "still in the backlog" — not yet planned into a sprint.</summary>
    public string? SprintId { get; private set; }

    /// <summary>Tracks whether US-120's due-soon reminder has already fired for
    /// the CURRENT due date — reset to null whenever the due date changes, so a
    /// rescheduled item gets its own fresh reminder instead of staying silent.</summary>
    public DateTime? ReminderSentOn { get; private set; }

    private readonly List<ChecklistItem> _checklistItems = new();
    public IReadOnlyList<ChecklistItem> ChecklistItems => _checklistItems.OrderBy(i => i.Order).ToList();

    /// <summary>References Team.Labels by id — the label's own name/color lives
    /// only on the Team, so renaming/recoloring a label doesn't require touching every story.</summary>
    private readonly List<string> _labelIds = new();
    public IReadOnlyList<string> LabelIds => _labelIds.AsReadOnly();

    /// <summary>Null means "does not recur". See CreateNextOccurrence for how a
    /// completed recurring story spawns its follow-up (US-129).</summary>
    public RecurrenceFrequency? RecurrenceFrequency { get; private set; }
    public DateTime? RecurrenceEndDate { get; private set; }

    private readonly List<Attachment> _attachments = new();
    public IReadOnlyList<Attachment> Attachments => _attachments.AsReadOnly();

    /// <summary>10 MB, per US-134's AC ("e.g., 10 MB") — same limit enforced
    /// again server-side in AddAttachmentCommandHandler regardless of what a
    /// client claims, since a client-side check alone can't be trusted.</summary>
    public const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;

    /// <summary>US-137: an estimated effort in hours. Null means "not estimated yet".</summary>
    public double? EstimatedHours { get; private set; }

    private readonly List<TimeLogEntry> _timeLogEntries = new();
    public IReadOnlyList<TimeLogEntry> TimeLogEntries => _timeLogEntries.AsReadOnly();

    private readonly List<StoryLink> _links = new();
    public IReadOnlyList<StoryLink> Links => _links.AsReadOnly();

    /// <summary>US-138 AC: "a running total of logged time".</summary>
    public double TotalLoggedHours => _timeLogEntries.Sum(t => t.Hours);

    /// <summary>Who created this story — shown as "Reporter" (Jira's term) on the detail page. Nullable only because stories created before this field existed have no value on file.</summary>
    public string? CreatedByUserId { get; private set; }

    /// <summary>
    /// References another UserStory's Id — this story is a subtask of that
    /// one (Jira's model: subtasks are lightweight child work items, and a
    /// subtask can't itself have subtasks — enforced in CreateSubtask, not
    /// here, since a subtask is otherwise a completely ordinary UserStory
    /// with its own status/assignee/etc). Null means this is a normal,
    /// top-level story.
    /// </summary>
    public string? ParentId { get; private set; }

    /// <summary>The Jira issue key (e.g. "KAN-3") this story was imported from — null for stories created directly in Eunomia. Lets re-importing the same project update existing stories instead of creating duplicates; see UserStoryRowApplier.</summary>
    public string? JiraIssueKey { get; private set; }

    /// <summary>
    /// References another UserStory's Id — that story is this one's Epic.
    /// Unlike ParentId (subtasks), an epic link never hides a story from the
    /// normal backlog/board (SearchAsync doesn't filter on this) — a story
    /// under an epic is still an ordinary top-level backlog item, just
    /// grouped under a bigger piece of work. The epic itself is just another
    /// ordinary UserStory (Eunomia has no separate "Epic" type), so this can
    /// point at any story, imported from Jira or not.
    /// </summary>
    public string? EpicId { get; private set; }

    private UserStory() { }

    private UserStory(string id, string teamId, string title, string? description) : base(id)
    {
        TeamId = teamId;
        Title = title;
        Description = description;
        CreatedOn = DateTime.UtcNow;
    }

    public static UserStory Create(string id, string teamId, string title, string? description, string? createdByUserId = null, string? parentId = null, string? jiraIssueKey = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new UserStory(id, teamId, title.Trim(), description?.Trim())
        {
            CreatedByUserId = createdByUserId,
            ParentId = parentId,
            JiraIssueKey = jiraIssueKey
        };
    }

    /// <summary>
    /// Reconstructs a UserStory from persisted data. Used by
    /// UserStoryRepository's ToDomain mapping — see Team.Rehydrate for the
    /// same pattern and why it exists (keeps the persistence document a
    /// plain shape while the aggregate keeps controlled mutation).
    /// </summary>
    public static UserStory Rehydrate(
        string id,
        string teamId,
        string title,
        string? description,
        UserStoryStatus status,
        UserStoryPriority priority,
        string? assigneeId,
        DateTime? dueDate,
        DateTime createdOn,
        int version,
        bool isArchived,
        int? storyPoints,
        string? sprintId,
        IEnumerable<ChecklistItem>? checklistItems = null,
        DateTime? reminderSentOn = null,
        IEnumerable<string>? labelIds = null,
        RecurrenceFrequency? recurrenceFrequency = null,
        DateTime? recurrenceEndDate = null,
        IEnumerable<Attachment>? attachments = null,
        double? estimatedHours = null,
        IEnumerable<TimeLogEntry>? timeLogEntries = null,
        IEnumerable<StoryLink>? links = null,
        string? createdByUserId = null,
        string? parentId = null,
        string? jiraIssueKey = null,
        string? epicId = null)
    {
        var story = new UserStory(id, teamId, title, description)
        {
            Status = status,
            Priority = priority,
            AssigneeId = assigneeId,
            DueDate = dueDate,
            CreatedOn = createdOn,
            Version = version,
            IsArchived = isArchived,
            StoryPoints = storyPoints,
            SprintId = sprintId,
            ReminderSentOn = reminderSentOn,
            RecurrenceFrequency = recurrenceFrequency,
            RecurrenceEndDate = recurrenceEndDate,
            EstimatedHours = estimatedHours,
            CreatedByUserId = createdByUserId,
            ParentId = parentId,
            JiraIssueKey = jiraIssueKey,
            EpicId = epicId
        };

        if (links is not null)
            story._links.AddRange(links);

        if (checklistItems is not null)
            story._checklistItems.AddRange(checklistItems);

        if (labelIds is not null)
            story._labelIds.AddRange(labelIds);

        if (attachments is not null)
            story._attachments.AddRange(attachments);

        if (timeLogEntries is not null)
            story._timeLogEntries.AddRange(timeLogEntries);

        return story;
    }

    public void UpdateDetails(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        Version++;
    }

    public void ChangeStatus(UserStoryStatus newStatus)
    {
        // Previously restricted to a fixed adjacency graph (ToDo -> Analyze
        // -> Dev -> Test -> Done, with Debug branching off Test); relaxed to
        // allow any-to-any so a board card can be dragged straight to any
        // column instead of only the "next" one in the old workflow graph.
        Status = newStatus;
    }

    public void ChangePriority(UserStoryPriority priority) => Priority = priority;

    public void Assign(string? userId)
    {
        AssigneeId = userId;

        // US-118 AC: assignee is notified when a story is assigned to them
        // (unassigning — userId is null — raises no event, nothing to notify).
        if (userId is not null)
            RaiseDomainEvent(new UserStoryAssignedEvent(Id, Title, userId));
    }

    public void SetDueDate(DateTime? dueDate)
    {
        if (DueDate != dueDate)
            ReminderSentOn = null; // a new/changed due date deserves its own reminder

        DueDate = dueDate;
    }

    public void MarkReminderSent() => ReminderSentOn = DateTime.UtcNow;

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    public void SetStoryPoints(int? points)
    {
        if (points is < 0)
            throw new ArgumentException("Story points cannot be negative.", nameof(points));

        StoryPoints = points;
    }

    /// <summary>Assigns this story to a sprint, or back to the backlog if sprintId is null.</summary>
    public void MoveToSprint(string? sprintId) => SprintId = sprintId;

    public void SetEpic(string? epicId)
    {
        if (epicId == Id)
            throw new ArgumentException("A story can't be its own epic.", nameof(epicId));

        EpicId = epicId;
    }

    // --- Checklist (US-122/123/124) ---

    public ChecklistItem AddChecklistItem(string id, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Checklist item text is required.", nameof(text));

        var nextOrder = _checklistItems.Count == 0 ? 0 : _checklistItems.Max(i => i.Order) + 1;
        var item = new ChecklistItem(id, text.Trim(), nextOrder);
        _checklistItems.Add(item);
        return item;
    }

    public void ToggleChecklistItem(string itemId)
    {
        var item = _checklistItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Checklist item not found.");
        item.Toggle();
    }

    public void RemoveChecklistItem(string itemId)
    {
        var item = _checklistItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Checklist item not found.");
        _checklistItems.Remove(item);
    }

    /// <summary>Reassigns Order to match the given sequence of ids — used for drag/move-based reordering (US-122 AC).</summary>
    public void ReorderChecklistItems(IReadOnlyList<string> orderedItemIds)
    {
        for (var i = 0; i < orderedItemIds.Count; i++)
        {
            var item = _checklistItems.FirstOrDefault(x => x.Id == orderedItemIds[i]);
            item?.SetOrder(i);
        }
    }

    // --- Labels (US-126) ---

    public void AddLabel(string labelId)
    {
        if (!_labelIds.Contains(labelId))
            _labelIds.Add(labelId);
    }

    public void RemoveLabel(string labelId) => _labelIds.Remove(labelId);

    // --- Recurrence (US-128/129/130) ---

    /// <summary>Frequency null turns recurrence off (US-130's "cancel" case) — future
    /// occurrences stop, but this doesn't retroactively touch any already-created ones.</summary>
    public void SetRecurrence(RecurrenceFrequency? frequency, DateTime? endDate)
    {
        RecurrenceFrequency = frequency;
        RecurrenceEndDate = frequency is null ? null : endDate;
    }

    /// <summary>
    /// Called when a recurring story is marked Done (see ChangeUserStoryStatusCommandHandler).
    /// Returns null if this story isn't recurring, or if the recurrence's end
    /// date has passed — in either case, no new occurrence should be spawned.
    /// The new occurrence carries over title/description/assignee/recurrence
    /// settings (US-129 AC) and computes its own due date one interval past
    /// this one's (if this one had a due date at all).
    /// </summary>
    public UserStory? CreateNextOccurrence(string newId)
    {
        if (RecurrenceFrequency is null) return null;
        if (RecurrenceEndDate.HasValue && DateTime.UtcNow >= RecurrenceEndDate.Value) return null;

        var next = new UserStory(newId, TeamId, Title, Description)
        {
            DueDate = DueDate.HasValue ? AddInterval(DueDate.Value, RecurrenceFrequency.Value) : null,
            RecurrenceFrequency = RecurrenceFrequency,
            RecurrenceEndDate = RecurrenceEndDate
        };

        if (AssigneeId is not null)
            next.Assign(AssigneeId); // raises UserStoryAssignedEvent — the assignee is notified about the new occurrence too

        return next;
    }

    // Fully-qualified on purpose: this class also has an instance property
    // named RecurrenceFrequency, and inside a static method a bare
    // "RecurrenceFrequency.Daily" gets misresolved as an attempt to access
    // that instance property (CS0120) rather than the enum type.
    private static DateTime AddInterval(DateTime from, global::TodoApp.Domain.UserStories.RecurrenceFrequency frequency) => frequency switch
    {
        global::TodoApp.Domain.UserStories.RecurrenceFrequency.Daily => from.AddDays(1),
        global::TodoApp.Domain.UserStories.RecurrenceFrequency.Weekly => from.AddDays(7),
        global::TodoApp.Domain.UserStories.RecurrenceFrequency.Monthly => from.AddMonths(1),
        _ => from
    };

    // --- Attachments (US-134/135/136) ---

    public Attachment AddAttachment(string id, string fileName, string contentType, long sizeBytes, string storageKey, string uploadedByUserId)
    {
        if (sizeBytes > MaxAttachmentSizeBytes)
            throw new ArgumentException($"File exceeds the {MaxAttachmentSizeBytes / (1024 * 1024)} MB limit.", nameof(sizeBytes));

        var attachment = new Attachment(id, fileName, contentType, sizeBytes, storageKey, uploadedByUserId, DateTime.UtcNow);
        _attachments.Add(attachment);
        return attachment;
    }

    public void RemoveAttachment(string attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new KeyNotFoundException("Attachment not found.");
        _attachments.Remove(attachment);
    }

    // --- Time tracking (US-137/138/139) ---

    public void SetEstimate(double? hours)
    {
        if (hours is < 0)
            throw new ArgumentException("Estimate cannot be negative.", nameof(hours));

        EstimatedHours = hours;
    }

    public TimeLogEntry LogTime(string id, double hours, string? note, string loggedByUserId)
    {
        if (hours <= 0)
            throw new ArgumentException("Logged hours must be greater than zero.", nameof(hours));

        var entry = new TimeLogEntry(id, hours, note?.Trim(), loggedByUserId, DateTime.UtcNow);
        _timeLogEntries.Add(entry);
        return entry;
    }

    // --- Story links / relationships (classic "linked issues") ---

    /// <summary>Adds (or replaces) this side of a link — the symmetric other
    /// side (e.g. the linked story's "BlockedBy" when this one gets "Blocks")
    /// is the caller's responsibility (see AddStoryLinkCommandHandler), since
    /// this aggregate has no access to the other UserStory.</summary>
    public void AddLink(string linkedStoryId, StoryLinkType linkType)
    {
        if (linkedStoryId == Id)
            throw new ArgumentException("A story can't link to itself.", nameof(linkedStoryId));

        _links.RemoveAll(l => l.LinkedStoryId == linkedStoryId);
        _links.Add(new StoryLink(linkedStoryId, linkType));
    }

    public void RemoveLink(string linkedStoryId) => _links.RemoveAll(l => l.LinkedStoryId == linkedStoryId);
}
