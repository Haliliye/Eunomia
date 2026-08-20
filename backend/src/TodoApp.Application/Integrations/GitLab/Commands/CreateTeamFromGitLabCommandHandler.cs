using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public class CreateTeamFromGitLabCommandHandler : IRequestHandler<CreateTeamFromGitLabCommand, CreateTeamFromGitLabResult>
{
    private readonly GitLabAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly GitLabProjectImportService _importService;
    private readonly IMediator _mediator;

    public CreateTeamFromGitLabCommandHandler(
        GitLabAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        GitLabProjectImportService importService,
        IMediator mediator)
    {
        _accessTokenProvider = accessTokenProvider;
        _teamRepository = teamRepository;
        _importService = importService;
        _mediator = mediator;
    }

    public async Task<CreateTeamFromGitLabResult> Handle(CreateTeamFromGitLabCommand request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);

        var teamName = !string.IsNullOrWhiteSpace(request.TeamName) ? request.TeamName.Trim() : request.ProjectName;

        var nameTaken = await _teamRepository.ExistsWithNameForUserAsync(teamName, request.RequestingUserId, cancellationToken);
        if (nameTaken)
            throw new InvalidOperationException($"You already have a team named \"{teamName}\". Choose a different name and try again.");

        var team = Team.Create(Guid.NewGuid().ToString(), teamName, $"Imported from GitLab project {request.PathWithNamespace}.", request.RequestingUserId);
        await _teamRepository.AddAsync(team, cancellationToken);
        await _mediator.PublishDomainEventsAsync(team, cancellationToken);

        var summary = await _importService.ImportAsync(team, accessToken, request.ProjectId, request.PathWithNamespace, request.RequestingUserId, cancellationToken);

        return new CreateTeamFromGitLabResult(TeamMapper.ToDto(team), summary);
    }
}
