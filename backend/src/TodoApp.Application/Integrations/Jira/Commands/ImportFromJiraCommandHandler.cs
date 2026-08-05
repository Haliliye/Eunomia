using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.Jira;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class ImportFromJiraCommandHandler : IRequestHandler<ImportFromJiraCommand, ImportSummaryDto>
{
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly IJiraClient _jiraClient;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ImportFromJiraCommandHandler(
        JiraAccessTokenProvider accessTokenProvider,
        IJiraClient jiraClient,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _accessTokenProvider = accessTokenProvider;
        _jiraClient = jiraClient;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ImportSummaryDto> Handle(ImportFromJiraCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        // Same permission level as the CSV import and sprint management — a
        // bulk-creation action, not open to every member.
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        var issues = await _jiraClient.GetIssuesAsync(accessToken, connection.CloudId, request.ProjectKey, cancellationToken);
        var rows = JiraIssueMapper.MapAndValidate(issues);

        var createdCount = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(createdCount, skippedCount, rows);
    }
}
