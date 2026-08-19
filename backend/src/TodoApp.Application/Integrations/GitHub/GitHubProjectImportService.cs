using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Integrations.GitHub;

/// <summary>
/// The actual "apply a GitHub repo's issues to a team" work — same overall
/// shape as JiraProjectImportService, deliberately scoped down to what
/// GitHub issues actually have: stories (create-or-update, keyed by
/// GitHubIssueKey), comments, labels, and unregistered-assignee invitations.
/// No attachments, links, or sprints for this first version — GitHub issues
/// don't have a native equivalent to Jira's issue links or sprints, and
/// mapping milestones to sprints or scraping ![image] attachments out of
/// issue bodies is a reasonable follow-up, not a must-have for v1.
/// </summary>
public class GitHubProjectImportService
{
    private const string DefaultLabelColor = "#94A3B8";

    private readonly IGitHubClient _gitHubClient;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<GitHubProjectImportService> _logger;

    public GitHubProjectImportService(
        IGitHubClient gitHubClient,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        ICommentRepository commentRepository,
        IEmailSignupInvitationRepository signupInvitationRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IRealtimeNotifier realtimeNotifier,
        ILogger<GitHubProjectImportService> logger)
    {
        _gitHubClient = gitHubClient;
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

    public async Task<ImportSummaryDto> ImportAsync(Team team, string accessToken, string owner, string repo, string requestingUserId, CancellationToken cancellationToken)
    {
        var issues = await _gitHubClient.GetIssuesAsync(accessToken, owner, repo, cancellationToken);

        // Resolved once per distinct assignee login (not once per issue) —
        // costs one extra API call per unique assignee, and is very often
        // null anyway (see IGitHubClient.GetUserEmailAsync), but it's the
        // only way an imported issue ever ends up auto-assigned in Eunomia.
        var distinctLogins = issues.Where(i => i.AssigneeLogin is not null).Select(i => i.AssigneeLogin!).Distinct().ToList();
        var emailByLogin = new Dictionary<string, string?>();
        foreach (var login in distinctLogins)
            emailByLogin[login] = await _gitHubClient.GetUserEmailAsync(accessToken, login, cancellationToken);

        // Auto-create any label on the repo's issues that this team doesn't
        // already have — same reasoning as the Jira/Azure DevOps importers:
        // a label with no matching team label is otherwise silently dropped.
        var existingLabelNames = team.Labels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var labelName in GitHubIssueMapper.DistinctLabelNames(issues))
        {
            if (existingLabelNames.Contains(labelName)) continue;
            team.CreateLabel(Guid.NewGuid().ToString(), labelName, DefaultLabelColor, requestingUserId);
            existingLabelNames.Add(labelName);
        }
        await _teamRepository.UpdateAsync(team, cancellationToken);

        var rows = GitHubIssueMapper.MapAndValidate(issues, owner, repo, emailByLogin);
        var applyResult = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, requestingUserId, cancellationToken);

        await ImportCommentsAsync(issues, applyResult.StoryIdByGitHubIssueKey, owner, repo, accessToken, requestingUserId, cancellationToken);
        await InviteUnregisteredAssigneesAsync(team, emailByLogin.Values, requestingUserId, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(applyResult.CreatedCount, skippedCount, rows, applyResult.UpdatedCount);
    }

    private async Task ImportCommentsAsync(IReadOnlyList<GitHubIssueDto> issues, IReadOnlyDictionary<string, string> storyIdByGitHubKey, string owner, string repo, string accessToken, string requestingUserId, CancellationToken cancellationToken)
    {
        foreach (var issue in issues)
        {
            var issueKey = $"{owner}/{repo}#{issue.Number}";
            if (!storyIdByGitHubKey.TryGetValue(issueKey, out var storyId)) continue;

            // Re-running an import would otherwise re-post every comment
            // again each time — skip stories that already have at least one
            // comment on file, same trade-off as the Jira importer.
            var existingComments = await _commentRepository.GetByUserStoryIdAsync(storyId, cancellationToken);
            if (existingComments.Count > 0) continue;

            IReadOnlyList<GitHubCommentDto> comments;
            try
            {
                comments = await _gitHubClient.GetCommentsAsync(accessToken, owner, repo, issue.Number, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch GitHub comments for issue {IssueKey}", issueKey);
                continue;
            }

            foreach (var comment in comments)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(comment.Body)) continue;

                    var content = $"_Originally posted by @{comment.AuthorLogin} on GitHub ({comment.CreatedOn:yyyy-MM-dd}):_\n\n{comment.Body}";
                    if (content.Length > 4000) content = content[..4000];

                    var entity = Comment.Create(Guid.NewGuid().ToString(), storyId, requestingUserId, content, mentionedUserIds: Array.Empty<string>());
                    await _commentRepository.AddAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import a comment onto story {StoryId} (GitHub issue {IssueKey})", storyId, issueKey);
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
