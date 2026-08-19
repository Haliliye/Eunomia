using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public class ImportFromGitHubCommandHandler : IRequestHandler<ImportFromGitHubCommand, ImportSummaryDto>
{
    private readonly GitHubAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly GitHubProjectImportService _importService;

    public ImportFromGitHubCommandHandler(
        GitHubAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        GitHubProjectImportService importService)
    {
        _accessTokenProvider = accessTokenProvider;
        _teamRepository = teamRepository;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(ImportFromGitHubCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        // Same permission level as the CSV/Jira/Azure DevOps imports — a
        // bulk-creation action, not open to every member.
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var (_, accessToken) = await _accessTokenProvider.GetAccessTokenAsync(request.RequestingUserId, cancellationToken);

        return await _importService.ImportAsync(team, accessToken, request.Owner, request.Repo, request.RequestingUserId, cancellationToken);
    }
}
