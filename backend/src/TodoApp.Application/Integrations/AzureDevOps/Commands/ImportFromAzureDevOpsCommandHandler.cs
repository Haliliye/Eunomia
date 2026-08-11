using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class ImportFromAzureDevOpsCommandHandler : IRequestHandler<ImportFromAzureDevOpsCommand, ImportSummaryDto>
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;
    private readonly ITokenCipher _tokenCipher;
    private readonly ITeamRepository _teamRepository;
    private readonly AzureDevOpsProjectImportService _importService;

    public ImportFromAzureDevOpsCommandHandler(
        IAzureDevOpsConnectionRepository connectionRepository,
        ITokenCipher tokenCipher,
        ITeamRepository teamRepository,
        AzureDevOpsProjectImportService importService)
    {
        _connectionRepository = connectionRepository;
        _tokenCipher = tokenCipher;
        _teamRepository = teamRepository;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(ImportFromAzureDevOpsCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Azure DevOps is not connected for this user.");
        var pat = _tokenCipher.Decrypt(connection.PersonalAccessTokenEncrypted);

        return await _importService.ImportAsync(team, connection.OrganizationName, request.ProjectName, pat, request.RequestingUserId, cancellationToken);
    }
}
