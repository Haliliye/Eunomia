using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public class GetAzureDevOpsStatusQueryHandler : IRequestHandler<GetAzureDevOpsStatusQuery, AzureDevOpsStatusDto>
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;

    public GetAzureDevOpsStatusQueryHandler(IAzureDevOpsConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task<AzureDevOpsStatusDto> Handle(GetAzureDevOpsStatusQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken);
        return connection is null
            ? new AzureDevOpsStatusDto(false, null, null)
            : new AzureDevOpsStatusDto(true, connection.OrganizationName, connection.ConnectedOn);
    }
}
