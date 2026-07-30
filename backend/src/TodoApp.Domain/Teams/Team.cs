using TodoApp.Domain.Common;

namespace TodoApp.Domain.Teams;

/// <summary>
/// Aggregate root for team management (EPIC-1). Owns team membership and
/// enforces the invariants around it (unique membership, owner protection).
/// </summary>
public class Team : AggregateRoot
{
    private readonly List<TeamMember> _members = new();
    private readonly List<Label> _labels = new();
    private readonly List<ColumnWipLimit> _wipLimits = new();
    private readonly List<StoryTemplate> _templates = new();

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<Label> Labels => _labels.AsReadOnly();
    public IReadOnlyCollection<ColumnWipLimit> WipLimits => _wipLimits.AsReadOnly();
    public IReadOnlyCollection<StoryTemplate> Templates => _templates.AsReadOnly();

    private Team() { }

    private Team(string id, string name, string? description) : base(id)
    {
        Name = name;
        Description = description;
    }

    public static Team Create(string id, string name, string? description, string ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name cannot be empty.", nameof(name));

        if (name.Length > 50)
            throw new ArgumentException("Team name cannot exceed 50 characters.", nameof(name));

        var team = new Team(id, name.Trim(), description?.Trim());
        team._members.Add(new TeamMember(ownerId, TeamRole.Owner, DateTime.UtcNow));
        team.RaiseDomainEvent(new TeamCreatedEvent(team.Id, ownerId));

        return team;
    }

    /// <summary>
    /// Reconstructs a Team from persisted data (e.g. a MongoDB document) without
    /// re-running creation invariants or raising domain events. Used by
    /// Infrastructure repositories — see TeamRepository's ToDomain mapping —
    /// so the persistence model can stay a plain document shape while the
    /// aggregate itself keeps private fields and controlled mutation.
    /// </summary>
    public static Team Rehydrate(
        string id, string name, string? description, IEnumerable<TeamMember> members,
        IEnumerable<Label>? labels = null, IEnumerable<ColumnWipLimit>? wipLimits = null,
        IEnumerable<StoryTemplate>? templates = null)
    {
        var team = new Team(id, name, description);
        team._members.AddRange(members);
        if (labels is not null) team._labels.AddRange(labels);
        if (wipLimits is not null) team._wipLimits.AddRange(wipLimits);
        if (templates is not null) team._templates.AddRange(templates);
        return team;
    }

    public void UpdateDetails(string name, string? description, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name cannot be empty.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void AddMember(string userId, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this team.");

        _members.Add(new TeamMember(userId, TeamRole.Member, DateTime.UtcNow));
    }

    /// <summary>
    /// Adds a member as the result of them accepting an invitation — no
    /// owner check, because the Invitation aggregate already verified the
    /// responding user is the one who was invited (see
    /// AcceptInvitationCommandHandler). Direct additions (no invitation)
    /// still go through AddMember above.
    /// </summary>
    public void AddMemberFromInvitation(string userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this team.");

        _members.Add(new TeamMember(userId, TeamRole.Member, DateTime.UtcNow));
    }

    public void RemoveMember(string userId, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            throw new InvalidOperationException("User is not a member of this team.");

        if (member.Role == TeamRole.Owner)
            throw new InvalidOperationException("Owner must transfer ownership before being removed.");

        _members.Remove(member);
        RaiseDomainEvent(new MemberRemovedEvent(Id, userId));
    }

    /// <summary>Owner-only: promotes a Member to Admin, or demotes an Admin
    /// back to Member. Can't be used to change the Owner's own role — that's
    /// what ownership transfer would be for, which this skeleton doesn't
    /// implement yet.</summary>
    public void SetMemberRole(string userId, TeamRole newRole, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        if (newRole == TeamRole.Owner)
            throw new ArgumentException("Use ownership transfer to change the owner, not this.", nameof(newRole));

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User is not a member of this team.");

        if (member.Role == TeamRole.Owner)
            throw new InvalidOperationException("The owner's role can't be changed this way.");

        if (newRole == TeamRole.Admin) member.PromoteToAdmin();
        else member.DemoteToMember();
    }

    // --- Labels (US-125) ---

    /// <summary>Owner-only, per US-125's AC — throws on a duplicate name within this team (case-insensitive).</summary>
    public Label CreateLabel(string id, string name, string color, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Label name is required.", nameof(name));

        if (_labels.Any(l => string.Equals(l.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A label named \"{name}\" already exists on this team.");

        var label = new Label(id, name.Trim(), color);
        _labels.Add(label);
        return label;
    }

    public void UpdateLabel(string labelId, string name, string color, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        var label = _labels.FirstOrDefault(l => l.Id == labelId)
            ?? throw new KeyNotFoundException("Label not found.");

        if (_labels.Any(l => l.Id != labelId && string.Equals(l.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A label named \"{name}\" already exists on this team.");

        label.Update(name.Trim(), color);
    }

    /// <summary>Removes the label from the team. Cascading its removal from
    /// every story that had it applied is handled by
    /// DeleteLabelCommandHandler (Application layer) — that needs
    /// IUserStoryRepository, which this aggregate deliberately has no access to.</summary>
    public void DeleteLabel(string labelId, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        var label = _labels.FirstOrDefault(l => l.Id == labelId)
            ?? throw new KeyNotFoundException("Label not found.");

        _labels.Remove(label);
    }

    // --- WIP limits (optional Kanban feature — owner-configurable) ---

    /// <summary>Limit null removes the cap for that column entirely (back to
    /// "no limit", the default for every column). This never blocks a status
    /// change — it's a visual signal on the board only (see
    /// ChangeUserStoryStatusCommandHandler, which doesn't check this at all).</summary>
    public void SetColumnWipLimit(string status, int? limit, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        var existing = _wipLimits.FirstOrDefault(w => w.Status == status);

        if (limit is null)
        {
            if (existing is not null) _wipLimits.Remove(existing);
            return;
        }

        if (limit < 1)
            throw new ArgumentException("WIP limit must be at least 1.", nameof(limit));

        if (existing is not null) existing.SetLimit(limit.Value);
        else _wipLimits.Add(new ColumnWipLimit(status, limit.Value));
    }

    // --- Story templates (owner-managed) ---

    public StoryTemplate CreateTemplate(string id, string name, string? defaultDescription, string? defaultPriority, IEnumerable<string> checklistItemTexts, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));

        var template = new StoryTemplate(id, name.Trim(), defaultDescription, defaultPriority, checklistItemTexts);
        _templates.Add(template);
        return template;
    }

    public void DeleteTemplate(string templateId, string requestingUserId)
    {
        EnsureIsOwner(requestingUserId);

        var template = _templates.FirstOrDefault(t => t.Id == templateId)
            ?? throw new KeyNotFoundException("Template not found.");
        _templates.Remove(template);
    }

    public bool IsMember(string userId) => _members.Any(m => m.UserId == userId);

    public bool IsOwnerOrAdmin(string userId) =>
        _members.Any(m => m.UserId == userId && (m.Role == TeamRole.Owner || m.Role == TeamRole.Admin));

    /// <summary>Baseline check for any team-scoped action — every UserStory/Sprint/Comment
    /// command should call this (via the story's TeamId) before doing anything, since
    /// otherwise any authenticated user could act on any team's data just by guessing an id.</summary>
    public void EnsureIsMember(string userId)
    {
        if (!IsMember(userId))
            throw new UnauthorizedAccessException("You're not a member of this team.");
    }

    /// <summary>For destructive/administrative actions (deleting a story, managing sprints) —
    /// stricter than plain membership but doesn't require full ownership.</summary>
    public void EnsureIsOwnerOrAdmin(string userId)
    {
        if (!IsOwnerOrAdmin(userId))
            throw new UnauthorizedAccessException("Only a team owner or admin can do this.");
    }

    private void EnsureIsOwner(string userId)
    {
        var isOwner = _members.Any(m => m.UserId == userId && m.Role == TeamRole.Owner);
        if (!isOwner)
            throw new UnauthorizedAccessException("Only the team owner can perform this action.");
    }
}
