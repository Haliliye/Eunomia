using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public class CreateTeamFromGitHubCommandHandler : IRequestHandler<CreateTeamFromGitHubCommand, CreateTeamFromGitHubResult>
{
    private readonly GitHubAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly GitHubProjectImportService _importService;
    private readonly IMediator _mediator;

    public CreateTeamFromGitHubCommandHandler(
        GitHubAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        GitHubProjectImportService importService,
        IMediator mediator)
    {
        _accessTokenProvider = accessTokenProvider;
        _teamRepository = teamRepository;
        _importService = importService;
        _mediator = mediator;
    }

    public async Task<CreateTeamFromGitHubResult> Handle(CreateTeamFromGitHubCommand request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await _accessTokenProvider.GetAccessTokenAsync(request.RequestingUserId, cancellationToken);

        var teamName = !string.IsNullOrWhiteSpace(request.TeamName) ? request.TeamName.Trim() : request.Repo;

        var nameTaken = await _teamRepository.ExistsWithNameForUserAsync(teamName, request.RequestingUserId, cancellationToken);
        if (nameTaken)
            throw new InvalidOperationException($"You already have a team named \"{teamName}\". Choose a different name and try again.");

        var team = Team.Create(Guid.NewGuid().ToString(), teamName, $"Imported from GitHub repo {request.Owner}/{request.Repo}.", request.RequestingUserId);
        await _teamRepository.AddAsync(team, cancellationToken);
        await _mediator.PublishDomainEventsAsync(team, cancellationToken);

        var summary = await _importService.ImportAsync(team, accessToken, request.Owner, request.Repo, request.RequestingUserId, cancellationToken);

        return new CreateTeamFromGitHubResult(TeamMapper.ToDto(team), summary);
    }
}
