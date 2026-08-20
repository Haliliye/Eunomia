using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public class ImportFromGitLabCommandHandler : IRequestHandler<ImportFromGitLabCommand, ImportSummaryDto>
{
    private readonly GitLabAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly GitLabProjectImportService _importService;

    public ImportFromGitLabCommandHandler(
        GitLabAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        GitLabProjectImportService importService)
    {
        _accessTokenProvider = accessTokenProvider;
        _teamRepository = teamRepository;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(ImportFromGitLabCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        // Same permission level as the CSV/Jira/Azure DevOps/GitHub imports —
        // a bulk-creation action, not open to every member.
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var (_, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);

        return await _importService.ImportAsync(team, accessToken, request.ProjectId, request.PathWithNamespace, request.RequestingUserId, cancellationToken);
    }
}
