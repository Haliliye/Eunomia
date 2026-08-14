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

namespace TodoApp.Application.Integrations.AzureDevOps;

/// <summary>
/// Mirrors TodoApp.Application.Integrations.Jira.JiraProjectImportService in
/// both structure and scope — sprints (iterations), story creation/update
/// (create-or-update by AzureDevOpsWorkItemId), issue links, comments,
/// attachments, epic (parent) hierarchy, unregistered-assignee invitations,
/// and sync-record bookkeeping. See that class for the reasoning behind each
/// piece; this is the Azure DevOps equivalent, PAT-authenticated instead of
/// OAuth.
/// </summary>
public class AzureDevOpsProjectImportService
{
    private const string DefaultLabelColor = "#94A3B8";

    private static readonly Dictionary<StoryLinkType, StoryLinkType> InverseLinkType = new()
    {
        [StoryLinkType.Blocks] = StoryLinkType.BlockedBy,
        [StoryLinkType.BlockedBy] = StoryLinkType.Blocks,
        [StoryLinkType.RelatesTo] = StoryLinkType.RelatesTo,
    };

    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip"
    };

    private readonly IAzureDevOpsClient _client;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly IAzureDevOpsProjectSyncRepository _syncRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<AzureDevOpsProjectImportService> _logger;

    public AzureDevOpsProjectImportService(
        IAzureDevOpsClient client,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        ISprintRepository sprintRepository,
        ICommentRepository commentRepository,
        IAttachmentStorage attachmentStorage,
        IEmailSignupInvitationRepository signupInvitationRepository,
        IAzureDevOpsProjectSyncRepository syncRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IRealtimeNotifier realtimeNotifier,
        ILogger<AzureDevOpsProjectImportService> logger)
    {
        _client = client;
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
    public async Task<ImportSummaryDto> ImportAsync(Team team, string organization, string projectName, string personalAccessToken, string requestingUserId, bool? setAutoSync, CancellationToken cancellationToken)
    {
        var workItems = await _client.GetWorkItemsAsync(personalAccessToken, organization, projectName, cancellationToken);

        var isOwner = team.Members.Any(m => m.UserId == requestingUserId && m.Role == TeamRole.Owner);

        var iterationIdByPath = await SyncIterationsAsync(team, personalAccessToken, organization, projectName, cancellationToken);
        var columnKeyByStateName = EnsureColumnsForStates(team, workItems, isOwner, requestingUserId);

        var rows = AzureDevOpsIssueMapper.MapAndValidate(workItems, columnKeyByStateName);

        var existingLabelNames = team.Labels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingLabelNames = isOwner
            ? AzureDevOpsIssueMapper.DistinctLabelNames(workItems).Where(name => !existingLabelNames.Contains(name)).ToList()
            : new List<string>();
        foreach (var name in missingLabelNames)
            team.CreateLabel(Guid.NewGuid().ToString(), name, DefaultLabelColor, requestingUserId);

        await _teamRepository.UpdateAsync(team, cancellationToken);

        var applyResult = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, requestingUserId, cancellationToken);
        var storyIdByWorkItemId = applyResult.StoryIdByAzureDevOpsWorkItemId;
        var skippedCount = rows.Count(r => !r.IsValid);

        await AssignIterationsAsync(workItems, storyIdByWorkItemId, iterationIdByPath, cancellationToken);
        await AssignParentsAsync(team, workItems, storyIdByWorkItemId, cancellationToken);
        await ImportLinksAsync(team, workItems, storyIdByWorkItemId, cancellationToken);
        await ImportCommentsAsync(workItems, storyIdByWorkItemId, organization, projectName, personalAccessToken, requestingUserId, cancellationToken);
        await ImportAttachmentsAsync(workItems, storyIdByWorkItemId, personalAccessToken, requestingUserId, cancellationToken);
        await InviteUnregisteredAssigneesAsync(team, workItems, requestingUserId, cancellationToken);
        await UpsertSyncRecordAsync(team.Id, organization, projectName, requestingUserId, setAutoSync, applyResult.CreatedCount, applyResult.UpdatedCount, skippedCount, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        return new ImportSummaryDto(applyResult.CreatedCount, skippedCount, rows, applyResult.UpdatedCount);
    }

    /// <summary>Creates/updates Eunomia Sprints from Azure DevOps' iteration tree, matched by leaf name. Returns a name -> Eunomia Sprint id map used by AssignIterationsAsync right after.</summary>
    private async Task<Dictionary<string, string>> SyncIterationsAsync(Team team, string personalAccessToken, string organization, string projectName, CancellationToken cancellationToken)
    {
        var iterations = await _client.GetIterationsAsync(personalAccessToken, organization, projectName, cancellationToken);
        var sprintIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (iterations.Count == 0) return sprintIdByName;

        var existingSprints = await _sprintRepository.GetByTeamIdAsync(team.Id, cancellationToken);
        var existingByName = existingSprints.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var iteration in iterations)
        {
            if (existingByName.TryGetValue(iteration.Name, out var existing))
            {
                sprintIdByName[iteration.Name] = existing.Id;
                continue;
            }

            // A "future" iteration with dates not yet set can't satisfy
            // Sprint.Create's endDate > startDate requirement — skipped
            // rather than guessing a date range.
            if (iteration.StartDate is null || iteration.FinishDate is null || iteration.FinishDate <= iteration.StartDate)
                continue;

            try
            {
                var sprint = Sprint.Create(Guid.NewGuid().ToString(), team.Id, iteration.Name, iteration.StartDate.Value, iteration.FinishDate.Value);
                // Whether this iteration is "active"/"past" isn't exposed by
                // the classification-nodes endpoint the way Jira's Agile API
                // exposes sprint state — every imported iteration starts as
                // Planned, same as a CSV-imported team's sprints would.
                await _sprintRepository.AddAsync(sprint, cancellationToken);
                sprintIdByName[iteration.Name] = sprint.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create sprint '{IterationName}' for team {TeamId}", iteration.Name, team.Id);
            }
        }

        return sprintIdByName;
    }

    private async Task AssignIterationsAsync(IReadOnlyList<AzureDevOpsWorkItemDto> workItems, IReadOnlyDictionary<string, string> storyIdByWorkItemId, IReadOnlyDictionary<string, string> sprintIdByName, CancellationToken cancellationToken)
    {
        foreach (var item in workItems)
        {
            if (item.IterationPath is null) continue;
            if (!storyIdByWorkItemId.TryGetValue(item.Id, out var storyId)) continue;

            // IterationPath is a full "Project\Release 1\Sprint 1" path —
            // matched by leaf name only, same simplification as Jira's sprint
            // matching (our Sprint domain has no path/hierarchy concept).
            var leafName = item.IterationPath.Split('\\').LastOrDefault() ?? item.IterationPath;
            if (!sprintIdByName.TryGetValue(leafName, out var sprintId)) continue;

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

    private static Dictionary<string, string> EnsureColumnsForStates(Team team, IReadOnlyList<AzureDevOpsWorkItemDto> workItems, bool isOwner, string requestingUserId)
    {
        var keyByStateName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stateName in workItems.Select(i => i.StateName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = team.Columns.FirstOrDefault(c => string.Equals(c.Name, stateName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                keyByStateName[stateName] = existing.Key;
                continue;
            }

            keyByStateName[stateName] = isOwner
                ? team.AddColumn(stateName, requestingUserId).Key
                : "ToDo";
        }

        return keyByStateName;
    }

    /// <summary>
    /// A work item's Hierarchy-Reverse relation (its parent, of any type —
    /// Azure DevOps doesn't distinguish "Epic parent" from any other parent
    /// the way Jira's Epic Link does) becomes this story's EpicId, resolving
    /// against this import batch first, then a DB lookup for a parent
    /// imported by an earlier run.
    /// </summary>
    private async Task AssignParentsAsync(Team team, IReadOnlyList<AzureDevOpsWorkItemDto> workItems, IReadOnlyDictionary<string, string> storyIdByWorkItemId, CancellationToken cancellationToken)
    {
        var parentIds = workItems.Select(i => i.ParentWorkItemId).Where(id => id is not null).Cast<string>().Distinct().ToList();
        if (parentIds.Count == 0) return;

        var unresolvedParentIds = parentIds.Where(id => !storyIdByWorkItemId.ContainsKey(id)).ToList();
        var resolvedFromDb = unresolvedParentIds.Count > 0
            ? (await _userStoryRepository.GetByAzureDevOpsWorkItemIdsAsync(team.Id, unresolvedParentIds, cancellationToken))
                .Where(s => s.AzureDevOpsWorkItemId is not null)
                .ToDictionary(s => s.AzureDevOpsWorkItemId!, s => s.Id)
            : new Dictionary<string, string>();

        foreach (var item in workItems)
        {
            if (item.ParentWorkItemId is null) continue;
            if (!storyIdByWorkItemId.TryGetValue(item.Id, out var storyId)) continue;

            var parentStoryId = storyIdByWorkItemId.TryGetValue(item.ParentWorkItemId, out var id) ? id : resolvedFromDb.GetValueOrDefault(item.ParentWorkItemId);
            if (parentStoryId is null || parentStoryId == storyId) continue;

            try
            {
                var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
                if (story is null || story.EpicId == parentStoryId) continue;

                story.SetEpic(parentStoryId);
                await _userStoryRepository.UpdateAsync(story, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set parent on story {StoryId} (work item {WorkItemId})", storyId, item.Id);
            }
        }
    }

    private static StoryLinkType MapLinkType(string relationType)
    {
        var lower = relationType.ToLowerInvariant();
        if (lower.Contains("dependen") && lower.Contains("predecessor")) return StoryLinkType.Blocks;
        if (lower.Contains("dependen") && lower.Contains("successor")) return StoryLinkType.BlockedBy;
        return StoryLinkType.RelatesTo;
    }

    private async Task ImportLinksAsync(Team team, IReadOnlyList<AzureDevOpsWorkItemDto> workItems, IReadOnlyDictionary<string, string> storyIdByWorkItemId, CancellationToken cancellationToken)
    {
        var unresolvedTargetIds = workItems.SelectMany(i => i.Links).Select(l => l.TargetWorkItemId)
            .Where(id => !storyIdByWorkItemId.ContainsKey(id)).Distinct().ToList();
        var resolvedFromDb = unresolvedTargetIds.Count > 0
            ? (await _userStoryRepository.GetByAzureDevOpsWorkItemIdsAsync(team.Id, unresolvedTargetIds, cancellationToken))
                .Where(s => s.AzureDevOpsWorkItemId is not null)
                .ToDictionary(s => s.AzureDevOpsWorkItemId!, s => s.Id)
            : new Dictionary<string, string>();

        string? Resolve(string workItemId) =>
            storyIdByWorkItemId.TryGetValue(workItemId, out var id) ? id : resolvedFromDb.GetValueOrDefault(workItemId);

        foreach (var item in workItems)
        {
            if (item.Links.Count == 0) continue;
            if (!storyIdByWorkItemId.TryGetValue(item.Id, out var storyId)) continue;

            var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
            if (story is null) continue;

            var changed = false;
            foreach (var link in item.Links)
            {
                var targetStoryId = Resolve(link.TargetWorkItemId);
                if (targetStoryId is null || targetStoryId == story.Id) continue;

                try
                {
                    var linkType = MapLinkType(link.RelationType);
                    var targetStory = await _userStoryRepository.GetByIdAsync(targetStoryId, cancellationToken);
                    if (targetStory is null) continue;

                    story.AddLink(targetStoryId, linkType);
                    targetStory.AddLink(story.Id, InverseLinkType[linkType]);
                    await _userStoryRepository.UpdateAsync(targetStory, cancellationToken);
                    changed = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import link {SourceId} -> {TargetId}", item.Id, link.TargetWorkItemId);
                }
            }

            if (changed) await _userStoryRepository.UpdateAsync(story, cancellationToken);
        }
    }

    private async Task ImportCommentsAsync(IReadOnlyList<AzureDevOpsWorkItemDto> workItems, IReadOnlyDictionary<string, string> storyIdByWorkItemId, string organization, string projectName, string personalAccessToken, string requestingUserId, CancellationToken cancellationToken)
    {
        foreach (var item in workItems)
        {
            if (!storyIdByWorkItemId.TryGetValue(item.Id, out var storyId)) continue;

            // Re-running an import (auto-sync) would otherwise re-post every
            // comment again each run — skip stories that already have at
            // least one comment on file, same trade-off as Jira's import.
            var existingComments = await _commentRepository.GetByUserStoryIdAsync(storyId, cancellationToken);
            if (existingComments.Count > 0) continue;

            IReadOnlyList<AzureDevOpsCommentDto> comments;
            try
            {
                comments = await _client.GetCommentsAsync(personalAccessToken, organization, projectName, item.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch comments for work item {WorkItemId}", item.Id);
                continue;
            }

            foreach (var comment in comments)
            {
                if (string.IsNullOrWhiteSpace(comment.Text)) continue;

                try
                {
                    var content = $"_Originally posted by {comment.AuthorDisplayName} on Azure DevOps ({comment.CreatedOn:yyyy-MM-dd}):_\n\n{comment.Text}";
                    if (content.Length > 4000) content = content[..4000];

                    var entity = Comment.Create(Guid.NewGuid().ToString(), storyId, requestingUserId, content, mentionedUserIds: Array.Empty<string>());
                    await _commentRepository.AddAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import a comment onto story {StoryId} (work item {WorkItemId})", storyId, item.Id);
                }
            }
        }
    }

    private async Task ImportAttachmentsAsync(IReadOnlyList<AzureDevOpsWorkItemDto> workItems, IReadOnlyDictionary<string, string> storyIdByWorkItemId, string personalAccessToken, string requestingUserId, CancellationToken cancellationToken)
    {
        foreach (var item in workItems)
        {
            if (item.Attachments.Count == 0) continue;
            if (!storyIdByWorkItemId.TryGetValue(item.Id, out var storyId)) continue;

            var story = await _userStoryRepository.GetByIdAsync(storyId, cancellationToken);
            if (story is null) continue;

            var existingFileNames = story.Attachments.Select(a => a.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var attachment in item.Attachments)
            {
                if (existingFileNames.Contains(attachment.FileName)) continue;

                var extension = Path.GetExtension(attachment.FileName);
                if (!AllowedAttachmentExtensions.Contains(extension)) continue;
                if (attachment.SizeBytes > UserStory.MaxAttachmentSizeBytes) continue;

                try
                {
                    using var content = await _client.DownloadAttachmentAsync(personalAccessToken, attachment.DownloadUrl, cancellationToken);
                    var storageKey = await _attachmentStorage.SaveAsync(content, cancellationToken);
                    story.AddAttachment(Guid.NewGuid().ToString(), attachment.FileName, attachment.ContentType, attachment.SizeBytes, storageKey, requestingUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import attachment '{FileName}' onto story {StoryId} (work item {WorkItemId})", attachment.FileName, storyId, item.Id);
                }
            }

            await _userStoryRepository.UpdateAsync(story, cancellationToken);
        }
    }

    /// <summary>Mirrors JiraProjectImportService.InviteUnregisteredAssigneesAsync — see that method for the reasoning.</summary>
    private async Task InviteUnregisteredAssigneesAsync(Team team, IReadOnlyList<AzureDevOpsWorkItemDto> workItems, string requestingUserId, CancellationToken cancellationToken)
    {
        if (!_emailSettings.IsConfigured) return;

        var inviter = await _userRepository.GetByIdAsync(requestingUserId, cancellationToken);
        var assigneeEmails = workItems
            .Select(i => i.AssigneeEmail)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var email in assigneeEmails)
        {
            try
            {
                var existingUser = await _userRepository.GetByEmailAsync(email!, cancellationToken);
                if (existingUser is not null) continue;

                if (await _signupInvitationRepository.ExistsAsync(email!, team.Id, cancellationToken))
                    continue;

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

    private async Task UpsertSyncRecordAsync(string teamId, string organization, string projectName, string requestingUserId, bool? setAutoSync, int createdCount, int updatedCount, int skippedCount, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _syncRepository.GetByTeamIdAsync(teamId, cancellationToken);
            if (existing is null)
            {
                var sync = AzureDevOpsProjectSync.Create(Guid.NewGuid().ToString(), teamId, projectName, requestingUserId);
                if (setAutoSync == true) sync.SetAutoSync(true, requestingUserId);
                sync.RecordSync(createdCount, updatedCount, skippedCount);
                await _syncRepository.AddAsync(sync, cancellationToken);
            }
            else
            {
                if (setAutoSync.HasValue) existing.SetAutoSync(setAutoSync.Value, requestingUserId);
                existing.RecordSync(createdCount, updatedCount, skippedCount);
                await _syncRepository.UpdateAsync(existing, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Azure DevOps sync state for team {TeamId}", teamId);
        }
    }
}
