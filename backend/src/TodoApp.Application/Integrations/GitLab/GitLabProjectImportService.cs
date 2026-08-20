using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Integrations.GitLab;

/// <summary>
/// The actual "apply a GitLab project's issues to a team" work — same
/// overall shape as GitHubProjectImportService, same v1 scope decision:
/// stories (create-or-update, keyed by GitLabIssueKey), comments (GitLab
/// calls them "notes"), labels, and unregistered-assignee invitations. No
/// attachments, links, or sprints/milestones for this first version.
/// </summary>
public class GitLabProjectImportService
{
    private const string DefaultLabelColor = "#94A3B8";

    private readonly IGitLabClient _gitLabClient;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<GitLabProjectImportService> _logger;

    public GitLabProjectImportService(
        IGitLabClient gitLabClient,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        ICommentRepository commentRepository,
        IEmailSignupInvitationRepository signupInvitationRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IRealtimeNotifier realtimeNotifier,
        ILogger<GitLabProjectImportService> logger)
    {
        _gitLabClient = gitLabClient;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _signupInvitationRepository = signupInvitationRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<ImportSummaryDto> ImportAsync(Team team, string accessToken, int projectId, string pathWithNamespace, string requestingUserId, CancellationToken cancellationToken)
    {
        var issues = await _gitLabClient.GetIssuesAsync(accessToken, projectId, cancellationToken);

        // Resolved once per distinct assignee username (not once per issue)
        // — costs one extra API call per unique assignee, and is very often
        // null anyway (see IGitLabClient.GetUserEmailAsync), but it's the
        // only way an imported issue ever ends up auto-assigned in Eunomia.
        var distinctUsernames = issues.Where(i => i.AssigneeUsername is not null).Select(i => i.AssigneeUsername!).Distinct().ToList();
        var emailByUsername = new Dictionary<string, string?>();
        foreach (var username in distinctUsernames)
            emailByUsername[username] = await _gitLabClient.GetUserEmailAsync(accessToken, username, cancellationToken);

        // Auto-create any label on the project's issues that this team
        // doesn't already have — same reasoning as the other importers.
        var existingLabelNames = team.Labels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var labelName in GitLabIssueMapper.DistinctLabelNames(issues))
        {
            if (existingLabelNames.Contains(labelName)) continue;
            team.CreateLabel(Guid.NewGuid().ToString(), labelName, DefaultLabelColor, requestingUserId);
            existingLabelNames.Add(labelName);
        }
        await _teamRepository.UpdateAsync(team, cancellationToken);

        var rows = GitLabIssueMapper.MapAndValidate(issues, pathWithNamespace, emailByUsername);
        var applyResult = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, requestingUserId, cancellationToken);

        await ImportNotesAsync(issues, applyResult.StoryIdByGitLabIssueKey, projectId, pathWithNamespace, accessToken, requestingUserId, cancellationToken);
        await InviteUnregisteredAssigneesAsync(team, emailByUsername.Values, requestingUserId, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(applyResult.CreatedCount, skippedCount, rows, applyResult.UpdatedCount);
    }

    private async Task ImportNotesAsync(IReadOnlyList<GitLabIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByGitLabKey, int projectId, string pathWithNamespace, string accessToken, string requestingUserId, CancellationToken cancellationToken)
    {
        foreach (var issue in issues)
        {
            var issueKey = $"{pathWithNamespace}#{issue.Iid}";
            if (!storyIdByGitLabKey.TryGetValue(issueKey, out var storyId)) continue;

            // Re-running an import would otherwise re-post every note again
            // each time — skip stories that already have at least one
            // comment on file, same trade-off as the other importers.
            var existingComments = await _commentRepository.GetByUserStoryIdAsync(storyId, cancellationToken);
            if (existingComments.Count > 0) continue;

            IReadOnlyList<GitLabNoteDto> notes;
            try
            {
                notes = await _gitLabClient.GetNotesAsync(accessToken, projectId, issue.Iid, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch GitLab notes for issue {IssueKey}", issueKey);
                continue;
            }

            foreach (var note in notes)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(note.Body)) continue;

                    var content = $"_Originally posted by @{note.AuthorUsername} on GitLab ({note.CreatedOn:yyyy-MM-dd}):_\n\n{note.Body}";
                    if (content.Length > 4000) content = content[..4000];

                    var entity = Comment.Create(Guid.NewGuid().ToString(), storyId, requestingUserId, content, mentionedUserIds: Array.Empty<string>());
                    await _commentRepository.AddAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import a note onto story {StoryId} (GitLab issue {IssueKey})", storyId, issueKey);
                }
            }
        }
    }

    private async Task InviteUnregisteredAssigneesAsync(Team team, IEnumerable<string?> assigneeEmails, string requestingUserId, CancellationToken cancellationToken)
    {
        if (!_emailSettings.IsConfigured) return; // no way to send anything — same gate RegisterCommandHandler uses

        var inviter = await _userRepository.GetByIdAsync(requestingUserId, cancellationToken);
        var emails = assigneeEmails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var email in emails)
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
}
