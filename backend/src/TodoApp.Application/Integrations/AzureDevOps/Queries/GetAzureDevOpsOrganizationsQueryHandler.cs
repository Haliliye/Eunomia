using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.AzureDevOps;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public class GetAzureDevOpsOrganizationsQueryHandler : IRequestHandler<GetAzureDevOpsOrganizationsQuery, IReadOnlyList<string>>
{
    private readonly AzureDevOpsAccessTokenProvider _accessTokenProvider;
    private readonly IAzureDevOpsClient _client;

    public GetAzureDevOpsOrganizationsQueryHandler(AzureDevOpsAccessTokenProvider accessTokenProvider, IAzureDevOpsClient client)
    {
        _accessTokenProvider = accessTokenProvider;
        _client = client;
    }

    public async Task<IReadOnlyList<string>> Handle(GetAzureDevOpsOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        return await _client.GetOrganizationsAsync(accessToken, cancellationToken);
    }
}
