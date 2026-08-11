using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.AzureDevOps;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public class GetAzureDevOpsProjectsQueryHandler : IRequestHandler<GetAzureDevOpsProjectsQuery, IReadOnlyList<AzureDevOpsProjectDto>>
{
    private readonly AzureDevOpsAccessTokenProvider _accessTokenProvider;
    private readonly IAzureDevOpsClient _client;

    public GetAzureDevOpsProjectsQueryHandler(AzureDevOpsAccessTokenProvider accessTokenProvider, IAzureDevOpsClient client)
    {
        _accessTokenProvider = accessTokenProvider;
        _client = client;
    }

    public async Task<IReadOnlyList<AzureDevOpsProjectDto>> Handle(GetAzureDevOpsProjectsQuery request, CancellationToken cancellationToken)
    {
        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        if (string.IsNullOrEmpty(connection.OrganizationName))
            throw new InvalidOperationException("No Azure DevOps organization selected yet.");

        return await _client.GetProjectsAsync(accessToken, connection.OrganizationName, cancellationToken);
    }
}
