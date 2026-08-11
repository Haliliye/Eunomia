using MediatR;
using TodoApp.Application.Integrations.AzureDevOps;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class CreateTeamFromAzureDevOpsCommandHandler : IRequestHandler<CreateTeamFromAzureDevOpsCommand, CreateTeamFromAzureDevOpsResult>
{
    private readonly AzureDevOpsAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly AzureDevOpsProjectImportService _importService;
    private readonly IMediator _mediator;

    public CreateTeamFromAzureDevOpsCommandHandler(
        AzureDevOpsAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        AzureDevOpsProjectImportService importService,
        IMediator mediator)
    {
        _accessTokenProvider = accessTokenProvider;
        _teamRepository = teamRepository;
        _importService = importService;
        _mediator = mediator;
    }

    public async Task<CreateTeamFromAzureDevOpsResult> Handle(CreateTeamFromAzureDevOpsCommand request, CancellationToken cancellationToken)
    {
        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        if (string.IsNullOrEmpty(connection.OrganizationName))
            throw new InvalidOperationException("No Azure DevOps organization selected yet.");

        var teamName = !string.IsNullOrWhiteSpace(request.TeamName) ? request.TeamName.Trim() : request.ProjectName;

        var nameTaken = await _teamRepository.ExistsWithNameForUserAsync(teamName, request.RequestingUserId, cancellationToken);
        if (nameTaken)
            throw new InvalidOperationException($"You already have a team named \"{teamName}\". Choose a different name and try again.");

        var team = Team.Create(Guid.NewGuid().ToString(), teamName, $"Imported from Azure DevOps project {request.ProjectName}.", request.RequestingUserId);
        await _teamRepository.AddAsync(team, cancellationToken);
        await _mediator.PublishDomainEventsAsync(team, cancellationToken);

        var summary = await _importService.ImportAsync(team, connection.OrganizationName, request.ProjectName, accessToken, request.RequestingUserId, cancellationToken);

        return new CreateTeamFromAzureDevOpsResult(TeamMapper.ToDto(team), summary);
    }
}
