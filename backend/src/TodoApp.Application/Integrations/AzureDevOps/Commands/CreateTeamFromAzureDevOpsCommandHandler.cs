using MediatR;
using TodoApp.Application.Integrations.AzureDevOps;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class CreateTeamFromAzureDevOpsCommandHandler : IRequestHandler<CreateTeamFromAzureDevOpsCommand, CreateTeamFromAzureDevOpsResult>
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;
    private readonly ITokenCipher _tokenCipher;
    private readonly ITeamRepository _teamRepository;
    private readonly AzureDevOpsProjectImportService _importService;
    private readonly IMediator _mediator;

    public CreateTeamFromAzureDevOpsCommandHandler(
        IAzureDevOpsConnectionRepository connectionRepository,
        ITokenCipher tokenCipher,
        ITeamRepository teamRepository,
        AzureDevOpsProjectImportService importService,
        IMediator mediator)
    {
        _connectionRepository = connectionRepository;
        _tokenCipher = tokenCipher;
        _teamRepository = teamRepository;
        _importService = importService;
        _mediator = mediator;
    }

    public async Task<CreateTeamFromAzureDevOpsResult> Handle(CreateTeamFromAzureDevOpsCommand request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Azure DevOps is not connected for this user.");
        var pat = _tokenCipher.Decrypt(connection.PersonalAccessTokenEncrypted);

        var teamName = !string.IsNullOrWhiteSpace(request.TeamName) ? request.TeamName.Trim() : request.ProjectName;

        var nameTaken = await _teamRepository.ExistsWithNameForUserAsync(teamName, request.RequestingUserId, cancellationToken);
        if (nameTaken)
            throw new InvalidOperationException($"You already have a team named \"{teamName}\". Choose a different name and try again.");

        var team = Team.Create(Guid.NewGuid().ToString(), teamName, $"Imported from Azure DevOps project {request.ProjectName}.", request.RequestingUserId);
        await _teamRepository.AddAsync(team, cancellationToken);
        await _mediator.PublishDomainEventsAsync(team, cancellationToken);

        var summary = await _importService.ImportAsync(team, connection.OrganizationName, request.ProjectName, pat, request.RequestingUserId, request.SetAutoSync, cancellationToken);

        return new CreateTeamFromAzureDevOpsResult(TeamMapper.ToDto(team), summary);
    }
}
