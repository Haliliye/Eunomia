using MediatR;
using TodoApp.Application.Integrations.AzureDevOps;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class ImportFromAzureDevOpsCommandHandler : IRequestHandler<ImportFromAzureDevOpsCommand, ImportSummaryDto>
{
    private readonly AzureDevOpsAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly AzureDevOpsProjectImportService _importService;

    public ImportFromAzureDevOpsCommandHandler(
        AzureDevOpsAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        AzureDevOpsProjectImportService importService)
    {
        _accessTokenProvider = accessTokenProvider;
        _teamRepository = teamRepository;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(ImportFromAzureDevOpsCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        if (string.IsNullOrEmpty(connection.OrganizationName))
            throw new InvalidOperationException("No Azure DevOps organization selected yet.");

        return await _importService.ImportAsync(team, connection.OrganizationName, request.ProjectName, accessToken, request.RequestingUserId, cancellationToken);
    }
}
