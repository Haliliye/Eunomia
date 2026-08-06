using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Integrations.Jira;

/// <summary>
/// The actual "apply a Jira project's issues to a team" work — label
/// auto-creation, row application, and unregistered-assignee invitations.
/// Extracted out of ImportFromJiraCommandHandler so CreateTeamFromJiraCommandHandler
/// (import into a brand-new team) can reuse the exact same logic instead of
/// duplicating it.
/// </summary>
public class JiraProjectImportService
{
    // Neutral slate — auto-created labels aren't guessed at a "meaningful"
    // color since Jira doesn't expose one; the team can recolor afterward.
    private const string DefaultLabelColor = "#94A3B8";

    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<JiraProjectImportService> _logger;

    public JiraProjectImportService(
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        IEmailSignupInvitationRepository signupInvitationRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IRealtimeNotifier realtimeNotifier,
        ILogger<JiraProjectImportService> logger)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _signupInvitationRepository = signupInvitationRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<ImportSummaryDto> ImportAsync(Team team, IReadOnlyList<JiraIssueDto> issues, string requestingUserId, CancellationToken cancellationToken)
    {
        var rows = JiraIssueMapper.MapAndValidate(issues);

        // CreateLabel is owner-only (stricter than the owner-or-admin check
        // callers already did before invoking this), so an admin importing
        // simply doesn't get missing labels auto-created — the rest of the
        // import still proceeds, same as when a matching label doesn't exist
        // at all today. On a brand-new team (CreateTeamFromJiraCommand) the
        // requesting user is always the owner (just-created it), so this
        // always applies there.
        var isOwner = team.Members.Any(m => m.UserId == requestingUserId && m.Role == TeamRole.Owner);
        var existingLabelNames = team.Labels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingLabelNames = isOwner
            ? JiraIssueMapper.DistinctLabelNames(issues).Where(name => !existingLabelNames.Contains(name)).ToList()
            : new List<string>();
        if (missingLabelNames.Count > 0)
        {
            foreach (var name in missingLabelNames)
                team.CreateLabel(Guid.NewGuid().ToString(), name, DefaultLabelColor, requestingUserId);
            await _teamRepository.UpdateAsync(team, cancellationToken);
        }

        var createdCount = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, requestingUserId, cancellationToken);

        await InviteUnregisteredAssigneesAsync(team, issues, requestingUserId, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(createdCount, skippedCount, rows);
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
}
