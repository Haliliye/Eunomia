using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class CreateTeamFromJiraCommandHandler : IRequestHandler<CreateTeamFromJiraCommand, CreateTeamFromJiraResult>
{
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly IJiraClient _jiraClient;
    private readonly ITeamRepository _teamRepository;
    private readonly JiraProjectImportService _importService;
    private readonly IMediator _mediator;

    public CreateTeamFromJiraCommandHandler(
        JiraAccessTokenProvider accessTokenProvider,
        IJiraClient jiraClient,
        ITeamRepository teamRepository,
        JiraProjectImportService importService,
        IMediator mediator)
    {
        _accessTokenProvider = accessTokenProvider;
        _jiraClient = jiraClient;
        _teamRepository = teamRepository;
        _importService = importService;
        _mediator = mediator;
    }

    public async Task<CreateTeamFromJiraResult> Handle(CreateTeamFromJiraCommand request, CancellationToken cancellationToken)
    {
        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);

        // Fetched primarily to default the team name to Jira's own project
        // name when the person doesn't type one — the actual issues come
        // from a separate call below.
        var projects = await _jiraClient.GetProjectsAsync(accessToken, connection.CloudId, cancellationToken);
        var project = projects.FirstOrDefault(p => p.Key == request.ProjectKey);

        var teamName = !string.IsNullOrWhiteSpace(request.TeamName) ? request.TeamName.Trim() : project?.Name ?? request.ProjectKey;

        var nameTaken = await _teamRepository.ExistsWithNameForUserAsync(teamName, request.RequestingUserId, cancellationToken);
        if (nameTaken)
            throw new InvalidOperationException($"You already have a team named \"{teamName}\". Choose a different name and try again.");

        var team = Team.Create(Guid.NewGuid().ToString(), teamName, $"Imported from Jira project {request.ProjectKey}.", request.RequestingUserId);
        await _teamRepository.AddAsync(team, cancellationToken);
        await _mediator.PublishDomainEventsAsync(team, cancellationToken);

        var summary = await _importService.ImportAsync(team, request.ProjectKey, accessToken, connection.CloudId, request.RequestingUserId, request.SetAutoSync, cancellationToken);

        return new CreateTeamFromJiraResult(TeamMapper.ToDto(team), summary);
    }
}
