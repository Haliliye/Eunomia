using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public class GetAzureDevOpsProjectsQueryHandler : IRequestHandler<GetAzureDevOpsProjectsQuery, IReadOnlyList<AzureDevOpsProjectDto>>
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;
    private readonly IAzureDevOpsClient _client;
    private readonly ITokenCipher _tokenCipher;

    public GetAzureDevOpsProjectsQueryHandler(IAzureDevOpsConnectionRepository connectionRepository, IAzureDevOpsClient client, ITokenCipher tokenCipher)
    {
        _connectionRepository = connectionRepository;
        _client = client;
        _tokenCipher = tokenCipher;
    }

    public async Task<IReadOnlyList<AzureDevOpsProjectDto>> Handle(GetAzureDevOpsProjectsQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Azure DevOps is not connected for this user.");

        var pat = _tokenCipher.Decrypt(connection.PersonalAccessTokenEncrypted);
        return await _client.GetProjectsAsync(pat, connection.OrganizationName, cancellationToken);
    }
}
