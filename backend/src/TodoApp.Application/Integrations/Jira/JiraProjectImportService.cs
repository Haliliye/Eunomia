using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Integrations.Jira;

/// <summary>
/// The actual "apply a Jira project's issues to a team" work: fetches
/// issues itself (callers just supply a token/cloudId), then applies
/// sprints, stories (create-or-update, keyed by JiraIssueKey), issue links,
/// comments, attachments, and unregistered-assignee invitations, and
/// finally records/refreshes a JiraProjectSync row (see that class for what
/// auto-sync means). Shared by ImportFromJiraCommandHandler,
/// CreateTeamFromJiraCommandHandler, and JiraAutoSyncBackgroundService — the
/// only thing that differs between those three callers is how the team got
/// chosen (existing team / brand-new team / periodic background loop), not
/// what happens once there's a team and Jira credentials.
/// </summary>
public class JiraProjectImportService
{
    // Neutral slate — auto-created labels aren't guessed at a "meaningful"
    // color since Jira doesn't expose one; the team can recolor afterward.
    private const string DefaultLabelColor = "#94A3B8";

    private static readonly Dictionary<StoryLinkType, StoryLinkType> InverseLinkType = new()
    {
        [StoryLinkType.Blocks] = StoryLinkType.BlockedBy,
        [StoryLinkType.BlockedBy] = StoryLinkType.Blocks,
        [StoryLinkType.RelatesTo] = StoryLinkType.RelatesTo,
    };

    // Mirrors AddAttachmentCommandHandler's allowlist — imported attachments
    // go through the exact same storage path as a manually uploaded one, so
    // the same restrictions apply (executables/scripts excluded on purpose).
    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip"
    };

    private readonly IJiraClient _jiraClient;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly IJiraProjectSyncRepository _syncRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<JiraProjectImportService> _logger;

    public JiraProjectImportService(
        IJiraClient jiraClient,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        ISprintRepository sprintRepository,
        ICommentRepository commentRepository,
        IAttachmentStorage attachmentStorage,
        IEmailSignupInvitationRepository signupInvitationRepository,
        IJiraProjectSyncRepository syncRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IRealtimeNotifier realtimeNotifier,
        ILogger<JiraProjectImportService> logger)
    {
        _jiraClient = jiraClient;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _sprintRepository = sprintRepository;
        _commentRepository = commentRepository;
        _attachmentStorage = attachmentStorage;
        _signupInvitationRepository = signupInvitationRepository;
        _syncRepository = syncRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    /// <param name="setAutoSync">Null leaves any existing auto-sync setting untouched (used by the background sync loop and plain re-imports); true/false explicitly turns it on/off (used when a person checks/unchecks "keep in sync" at import time).</param>
    public async Task<ImportSummaryDto> ImportAsync(
        Team team, string projectKey, string accessToken, string cloudId, string requestingUserId, bool? setAutoSync, CancellationToken cancellationToken)
    {
        var issues = await _jiraClient.GetIssuesAsync(accessToken, cloudId, projectKey, cancellationToken);

        var sprintIdByName = await SyncSprintsAsync(team, accessToken, cloudId, projectKey, cancellationToken);

        var isOwner = team.Members.Any(m => m.UserId == requestingUserId && m.Role == TeamRole.Owner);

        // Jira's own board tells us the "real" left-to-right status order
        // (e.g. Backlog, To Do, In Progress, In Review, QA, Done) — falls
        // back to whatever order issues happen to introduce new statuses in
        // if the project has no board to read this from (see
        // GetBoardStatusOrderAsync).
        var jiraStatusOrder = await _jiraClient.GetBoardStatusOrderAsync(accessToken, cloudId, projectKey, cancellationToken);

        // Every distinct Jira status becomes (or is matched to) a real board
        // column — a 9-status Jira workflow gets 9 real Eunomia columns
        // instead of being squeezed into the six defaults.
        var columnKeyByStatusName = EnsureColumnsForStatuses(team, issues, jiraStatusOrder, isOwner, requestingUserId);

        var rows = JiraIssueMapper.MapAndValidate(issues, columnKeyByStatusName);

        var existingLabelNames = team.Labels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingLabelNames = isOwner
            ? JiraIssueMapper.DistinctLabelNames(issues).Where(name => !existingLabelNames.Contains(name)).ToList()
            : new List<string>();
        foreach (var name in missingLabelNames)
            team.CreateLabel(Guid.NewGuid().ToString(), name, DefaultLabelColor, requestingUserId);

        // Reposition every column to match Jira's own board order — not just
        // the newly created ones, since an existing default column (say,
        // "Done") should also slot into wherever Jira puts its equivalent
        // rather than staying wherever it happened to be created. Any
        // Eunomia-only column with no Jira counterpart (a custom one, or a
        // default that this Jira project's workflow doesn't use) keeps its
        // relative position and is placed after all Jira-matched ones.
        if (isOwner && jiraStatusOrder.Count > 0)
        {
            var jiraOrderedKeys = jiraStatusOrder
                .Select(name => columnKeyByStatusName.GetValueOrDefault(name))
                .Where(key => key is not null)
                .Cast<string>()
                .Distinct()
                .ToList();
            var remainingKeys = team.Columns.Select(c => c.Key).Where(k => !jiraOrderedKeys.Contains(k)).ToList();
            team.ReorderColumns(jiraOrderedKeys.Concat(remainingKeys).ToList(), requestingUserId);
            _logger.LogInformation("Reordered board columns for team {TeamId} to match Jira: {Order}", team.Id, string.Join(", ", jiraOrderedKeys.Concat(remainingKeys)));
        }
        else
        {
            _logger.LogWarning("Skipped Jira column reordering for team {TeamId} (isOwner={IsOwner}, jiraStatusOrderCount={Count})", team.Id, isOwner, jiraStatusOrder.Count);
        }

        // One save for the new columns, new labels, and reordering — all
        // just mutated the same in-memory Team, so a single UpdateAsync covers it.
        await _teamRepository.UpdateAsync(team, cancellationToken);

        var applyResult = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, requestingUserId, cancellationToken);

        await AssignSprintsAsync(issues, applyResult.StoryIdByJiraKey, sprintIdByName, cancellationToken);
        await AssignEpicsAsync(team, issues, applyResult.StoryIdByJiraKey, cancellationToken);
        await ImportLinksAsync(team, issues, applyResult.StoryIdByJiraKey, cancellationToken);
        await ImportCommentsAsync(issues, applyResult.StoryIdByJiraKey, requestingUserId, cancellationToken);
        await ImportAttachmentsAsync(issues, applyResult.StoryIdByJiraKey, accessToken, requestingUserId, cancellationToken);
        await InviteUnregisteredAssigneesAsync(team, issues, requestingUserId, cancellationToken);
        await UpsertSyncRecordAsync(team.Id, projectKey, requestingUserId, setAutoSync, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(applyResult.CreatedCount, skippedCount, rows, applyResult.UpdatedCount);
    }

    /// <summary>Creates/updates Eunomia Sprints from Jira's board sprints, matched by name. Returns a name -> Eunomia Sprint id map used by AssignSprintsAsync right after.</summary>
    private async Task<Dictionary<string, string>> SyncSprintsAsync(Team team, string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken)
    {
        var jiraSprints = await _jiraClient.GetSprintsAsync(accessToken, cloudId, projectKey, cancellationToken);
        var sprintIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (jiraSprints.Count == 0) return sprintIdByName;

        var existingSprints = await _sprintRepository.GetByTeamIdAsync(team.Id, cancellationToken);
        var existingByName = existingSprints.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var jiraSprint in jiraSprints)
        {
            if (existingByName.TryGetValue(jiraSprint.Name, out var existing))
            {
                sprintIdByName[jiraSprint.Name] = existing.Id;
                continue;
            }

            // A sprint with no dates yet (rare, but possible for a "future"
            // sprint that was created but never scheduled) can't satisfy
            // Sprint.Create's endDate > startDate requirement — skipped
            // rather than guessing a date range.
            if (jiraSprint.StartDate is null || jiraSprint.EndDate is null || jiraSprint.EndDate <= jiraSprint.StartDate)
                continue;

            try
            {
                var sprint = Sprint.Create(Guid.NewGuid().ToString(), team.Id, jiraSprint.Name, jiraSprint.StartDate.Value, jiraSprint.EndDate.Value);
                // Burndown figures (totalPointsAtStart/completedPoints) can't
                // be reconstructed retroactively for a sprint that already
                // happened in Jira — 0 is a placeholder so Status at least
                // reflects reality; the burndown chart just won't have
                // meaningful history for these imported sprints.
                if (jiraSprint.State is "active" or "closed") sprint.Start(0);
                if (jiraSprint.State is "closed") sprint.Complete(0);

                await _sprintRepository.AddAsync(sprint, cancellationToken);
                sprintIdByName[jiraSprint.Name] = sprint.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create sprint '{SprintName}' for team {TeamId}", jiraSprint.Name, team.Id);
            }
        }

        return sprintIdByName;
    }

    /// <summary>
    /// Matches every distinct Jira status name on these issues to a real
    /// board column, creating one (named exactly after the Jira status) for
    /// any that don't already match an existing column by name — so a
    /// project with e.g. "Backlog / To Do / In Progress / In Review / QA /
    /// Blocked / Done" ends up with all seven, not squeezed into the six
    /// defaults. Matching is case-insensitive so re-running an import (or
    /// importing a second project into the same team) doesn't create
    /// duplicate columns for the same status spelled the same way.
    /// New columns are created walking jiraStatusOrder (Jira's own board
    /// order) rather than issues' encounter order, purely so that if two new
    /// columns are created in the same call they land in the right order
    /// *relative to each other* — ImportAsync's ReorderColumns pass right
    /// after this is what actually places everything (new and pre-existing)
    /// into Jira's full order, including past the default columns.
    /// AddColumn is owner-only (see Team.AddColumn) — an admin importing
    /// falls back to "ToDo" for any status with no existing matching column,
    /// rather than being blocked entirely.
    /// </summary>
    private static Dictionary<string, string> EnsureColumnsForStatuses(Team team, IReadOnlyList<JiraIssueDto> issues, IReadOnlyList<string> jiraStatusOrder, bool isOwner, string requestingUserId)
    {
        var distinctStatusNames = issues.Select(i => i.StatusName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        // Walk Jira's board order first (covers most statuses in the right
        // relative sequence), then anything left over (a status that exists
        // on issues but wasn't on the board's column config, e.g. a status
        // no longer wired to a column) in whatever order it was encountered.
        var orderedStatusNames = jiraStatusOrder
            .Where(name => distinctStatusNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Concat(distinctStatusNames.Where(name => !jiraStatusOrder.Contains(name, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var keyByStatusName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var statusName in orderedStatusNames)
        {
            var existing = team.Columns.FirstOrDefault(c => string.Equals(c.Name, statusName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                keyByStatusName[statusName] = existing.Key;
                continue;
            }

            keyByStatusName[statusName] = isOwner
                ? team.AddColumn(statusName, requestingUserId).Key
                : "ToDo";
        }

        return keyByStatusName;
    }

    private async Task AssignSprintsAsync(IReadOnlyList<JiraIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByJiraKey, IReadOnlyDictionary<string, string> sprintIdByName, CancellationToken cancellationToken)
    {
        foreach (var issue in issues)
        {
            if (issue.SprintName is null) continue;
            if (!storyIdByJiraKey.TryGetValue(issue.Key, out var storyId)) continue;
            if (!sprintIdByName.TryGetValue(issue.SprintName, out var sprintId)) continue;

            try
            {
                var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
                if (story is null || story.SprintId == sprintId) continue;

                story.MoveToSprint(sprintId);
                await _userStoryRepository.UpdateAsync(story, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign story {StoryId} to sprint {SprintId}", storyId, sprintId);
            }
        }
    }

    /// <summary>
    /// Resolves each issue's Epic Link/parent (see epicIssueKey in
    /// JiraApiClient) to a Eunomia story id and sets UserStory.EpicId — same
    /// resolution strategy as ImportLinksAsync (this batch first, then a DB
    /// lookup for epics imported by an earlier run).
    /// </summary>
    private async Task AssignEpicsAsync(Team team, IReadOnlyList<JiraIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByJiraKey, CancellationToken cancellationToken)
    {
        var epicKeys = issues.Select(i => i.EpicIssueKey).Where(k => k is not null).Cast<string>().Distinct().ToList();
        if (epicKeys.Count == 0) return;

        var unresolvedEpicKeys = epicKeys.Where(k => !storyIdByJiraKey.ContainsKey(k)).ToList();
        var resolvedFromDb = unresolvedEpicKeys.Count > 0
            ? (await _userStoryRepository.GetByJiraIssueKeysAsync(team.Id, unresolvedEpicKeys, cancellationToken))
                .Where(s => s.JiraIssueKey is not null)
                .ToDictionary(s => s.JiraIssueKey!, s => s.Id)
            : new Dictionary<string, string>();

        foreach (var issue in issues)
        {
            if (issue.EpicIssueKey is null) continue;
            if (!storyIdByJiraKey.TryGetValue(issue.Key, out var storyId)) continue;

            var epicStoryId = storyIdByJiraKey.TryGetValue(issue.EpicIssueKey, out var id) ? id : resolvedFromDb.GetValueOrDefault(issue.EpicIssueKey);
            if (epicStoryId is null || epicStoryId == storyId) continue; // epic not (yet) imported, or a self-reference — skip rather than guess

            try
            {
                var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
                if (story is null || story.EpicId == epicStoryId) continue;

                story.SetEpic(epicStoryId);
                await _userStoryRepository.UpdateAsync(story, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set epic on story {StoryId} (Jira issue {IssueKey})", storyId, issue.Key);
            }
        }
    }

    private static StoryLinkType MapJiraLinkType(string raw)
    {
        var lower = raw.ToLowerInvariant();
        // Jira's own vocabulary here is effectively unbounded (apps can add
        // custom link types like "duplicates"/"clones"/"causes") — anything
        // that isn't clearly a block relationship falls back to RelatesTo,
        // which is always a safe, non-lossy choice.
        if (lower.Contains("blocked")) return StoryLinkType.BlockedBy;
        if (lower.Contains("block")) return StoryLinkType.Blocks;
        return StoryLinkType.RelatesTo;
    }

    private async Task ImportLinksAsync(Team team, IReadOnlyList<JiraIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByJiraKey, CancellationToken cancellationToken)
    {
        // Resolve link targets outside this import batch too — the target
        // issue may have been imported by an earlier run and just wasn't
        // touched by this one (e.g. it's in a different sprint/JQL window).
        var unresolvedTargetKeys = issues.SelectMany(i => i.Links).Select(l => l.TargetIssueKey)
            .Where(k => !storyIdByJiraKey.ContainsKey(k)).Distinct().ToList();
        var resolvedFromDb = unresolvedTargetKeys.Count > 0
            ? (await _userStoryRepository.GetByJiraIssueKeysAsync(team.Id, unresolvedTargetKeys, cancellationToken))
                .Where(s => s.JiraIssueKey is not null)
                .ToDictionary(s => s.JiraIssueKey!, s => s.Id)
            : new Dictionary<string, string>();

        string? Resolve(string jiraKey) =>
            storyIdByJiraKey.TryGetValue(jiraKey, out var id) ? id : resolvedFromDb.GetValueOrDefault(jiraKey);

        foreach (var issue in issues)
        {
            if (issue.Links.Count == 0) continue;
            if (!storyIdByJiraKey.TryGetValue(issue.Key, out var storyId)) continue;

            var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
            if (story is null) continue;

            var changed = false;
            foreach (var link in issue.Links)
            {
                var targetStoryId = Resolve(link.TargetIssueKey);
                if (targetStoryId is null || targetStoryId == story.Id) continue; // target not (yet) imported, or a self-link — skip rather than guess

                try
                {
                    var linkType = MapJiraLinkType(link.LinkTypeRaw);
                    var targetStory = await _userStoryRepository.GetByIdAsync(targetStoryId, cancellationToken);
                    if (targetStory is null) continue;

                    story.AddLink(targetStoryId, linkType);
                    targetStory.AddLink(story.Id, InverseLinkType[linkType]);
                    await _userStoryRepository.UpdateAsync(targetStory, cancellationToken);
                    changed = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import link {SourceKey} -> {TargetKey}", issue.Key, link.TargetIssueKey);
                }
            }

            if (changed) await _userStoryRepository.UpdateAsync(story, cancellationToken);
        }
    }

    private async Task ImportCommentsAsync(IReadOnlyList<JiraIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByJiraKey, string requestingUserId, CancellationToken cancellationToken)
    {
        foreach (var issue in issues)
        {
            if (issue.Comments.Count == 0) continue;
            if (!storyIdByJiraKey.TryGetValue(issue.Key, out var storyId)) continue;

            // Re-running an import (auto-sync) would otherwise re-post every
            // comment again on each run — skip stories that already have at
            // least one comment on file, since we have no per-comment Jira
            // id to de-dupe against individually. Good enough for the common
            // case (import once, keep working in Eunomia from there).
            var existingComments = await _commentRepository.GetByUserStoryIdAsync(storyId, cancellationToken);
            if (existingComments.Count > 0) continue;

            foreach (var comment in issue.Comments)
            {
                try
                {
                    // The Jira author likely has no Eunomia account, so the
                    // comment is attributed to whoever ran the import, with
                    // the original author/date called out in the text itself.
                    var content = $"_Originally posted by {comment.AuthorDisplayName} on Jira ({comment.CreatedOn:yyyy-MM-dd}):_\n\n{comment.BodyText}";
                    if (content.Length > 4000) content = content[..4000];
                    if (string.IsNullOrWhiteSpace(comment.BodyText)) continue; // nothing came through the ADF walk (e.g. an image-only comment) — skip rather than post an empty one

                    var entity = Comment.Create(Guid.NewGuid().ToString(), storyId, requestingUserId, content, mentionedUserIds: Array.Empty<string>());
                    await _commentRepository.AddAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import a comment onto story {StoryId} (Jira issue {IssueKey})", storyId, issue.Key);
                }
            }
        }
    }

    private async Task ImportAttachmentsAsync(IReadOnlyList<JiraIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByJiraKey, string accessToken, string requestingUserId, CancellationToken cancellationToken)
    {
        foreach (var issue in issues)
        {
            if (issue.Attachments.Count == 0) continue;
            if (!storyIdByJiraKey.TryGetValue(issue.Key, out var storyId)) continue;

            var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
            if (story is null) continue;

            // Same de-dupe reasoning as comments — don't re-download and
            // re-attach the same files on every auto-sync run.
            var existingFileNames = story.Attachments.Select(a => a.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var attachment in issue.Attachments)
            {
                if (existingFileNames.Contains(attachment.FileName)) continue;

                var extension = Path.GetExtension(attachment.FileName);
                if (!AllowedAttachmentExtensions.Contains(extension)) continue; // same allowlist as a manual upload — unsupported types are silently skipped, not fatal to the import
                if (attachment.SizeBytes > UserStory.MaxAttachmentSizeBytes) continue;

                try
                {
                    using var content = await _jiraClient.DownloadAttachmentAsync(accessToken, attachment.DownloadUrl, cancellationToken);
                    var storageKey = await _attachmentStorage.SaveAsync(content, cancellationToken);
                    story.AddAttachment(Guid.NewGuid().ToString(), attachment.FileName, attachment.ContentType, attachment.SizeBytes, storageKey, requestingUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import attachment '{FileName}' onto story {StoryId} (Jira issue {IssueKey})", attachment.FileName, storyId, issue.Key);
                }
            }

            await _userStoryRepository.UpdateAsync(story, cancellationToken);
        }
    }

    /// <summary>
    /// A Jira assignee with no matching Eunomia account gets left unassigned
    /// by UserStoryRowApplier — this is the other half: email them a signup
    /// invitation so they can join and (once registered) get auto-added to
    /// this team, see RegisterCommandHandler. Best-effort: any single
    /// person's invite failing (bad email, SMTP hiccup) doesn't fail the
    /// import, which has already created the stories at this point.
    /// </summary>
    private async Task InviteUnregisteredAssigneesAsync(Team team, IReadOnlyList<JiraIssueDto> issues, string requestingUserId, CancellationToken cancellationToken)
    {
        if (!_emailSettings.IsConfigured) return; // no way to send anything — same gate RegisterCommandHandler uses

        var inviter = await _userRepository.GetByIdAsync(requestingUserId, cancellationToken);
        var assigneeEmails = issues
            .Select(i => i.AssigneeEmail)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var email in assigneeEmails)
        {
            try
            {
                var existingUser = await _userRepository.GetByEmailAsync(email!, cancellationToken);
                if (existingUser is not null) continue; // has an account already — UserStoryRowApplier's own lookup already handled assigning them

                if (await _signupInvitationRepository.ExistsAsync(email!, team.Id, cancellationToken))
                    continue; // already invited to this team by an earlier import — don't re-send every time

                var invitation = EmailSignupInvitation.Create(Guid.NewGuid().ToString(), email!, team.Id, requestingUserId);
                await _signupInvitationRepository.AddAsync(invitation, cancellationToken);

                var signupLink = $"{_emailSettings.FrontendBaseUrl}/register?email={Uri.EscapeDataString(email!)}";
                await _emailSender.SendAsync(email!, "You've been invited to Eunomia",
                    EmailTemplates.SignupInvitation(team.Name, inviter?.DisplayName ?? "A teammate", signupLink), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Eunomia signup invitation to {Email} for team {TeamId}", email, team.Id);
            }
        }
    }

    private async Task UpsertSyncRecordAsync(string teamId, string projectKey, string requestingUserId, bool? setAutoSync, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _syncRepository.GetByTeamIdAsync(teamId, cancellationToken);
            if (existing is null)
            {
                var sync = JiraProjectSync.Create(Guid.NewGuid().ToString(), teamId, projectKey, requestingUserId);
                if (setAutoSync == true) sync.SetAutoSync(true, requestingUserId);
                sync.MarkSynced();
                await _syncRepository.AddAsync(sync, cancellationToken);
            }
            else
            {
                if (setAutoSync.HasValue) existing.SetAutoSync(setAutoSync.Value, requestingUserId);
                existing.MarkSynced();
                await _syncRepository.UpdateAsync(existing, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Jira sync state for team {TeamId}", teamId);
        }
    }
}
