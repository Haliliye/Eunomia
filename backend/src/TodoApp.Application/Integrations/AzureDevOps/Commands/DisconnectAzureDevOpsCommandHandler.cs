using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class DisconnectAzureDevOpsCommandHandler : IRequestHandler<DisconnectAzureDevOpsCommand>
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;

    public DisconnectAzureDevOpsCommandHandler(IAzureDevOpsConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task Handle(DisconnectAzureDevOpsCommand request, CancellationToken cancellationToken) =>
        await _connectionRepository.DeleteByUserIdAsync(request.RequestingUserId, cancellationToken);
}
