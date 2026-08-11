using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Integrations.AzureDevOps;

/// <summary>
/// Mirrors TodoApp.Application.Integrations.Jira.JiraProjectImportService,
/// scoped down for this first version: work items (title, description,
/// state->column, priority, tags->labels, assignee, story points), no
/// comments/attachments/links/iterations/epics yet — see the equivalent
/// Jira features for what a follow-up pass could add here.
/// </summary>
public class AzureDevOpsProjectImportService
{
    private const string DefaultLabelColor = "#94A3B8";

    private readonly IAzureDevOpsClient _client;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<AzureDevOpsProjectImportService> _logger;

    public AzureDevOpsProjectImportService(
        IAzureDevOpsClient client,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        IEmailSignupInvitationRepository signupInvitationRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IRealtimeNotifier realtimeNotifier,
        ILogger<AzureDevOpsProjectImportService> logger)
    {
        _client = client;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _signupInvitationRepository = signupInvitationRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<ImportSummaryDto> ImportAsync(Team team, string organization, string projectName, string accessToken, string requestingUserId, CancellationToken cancellationToken)
    {
        var workItems = await _client.GetWorkItemsAsync(accessToken, organization, projectName, cancellationToken);

        var isOwner = team.Members.Any(m => m.UserId == requestingUserId && m.Role == TeamRole.Owner);

        // Every distinct Azure DevOps state becomes (or is matched to) a real
        // board column — same reasoning as Jira's EnsureColumnsForStatuses.
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

        await InviteUnregisteredAssigneesAsync(team, workItems, requestingUserId, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(applyResult.CreatedCount, skippedCount, rows, applyResult.UpdatedCount);
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
}
