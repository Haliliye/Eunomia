using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class SetAzureDevOpsOrganizationCommandHandler : IRequestHandler<SetAzureDevOpsOrganizationCommand>
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;

    public SetAzureDevOpsOrganizationCommandHandler(IAzureDevOpsConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task Handle(SetAzureDevOpsOrganizationCommand request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Azure DevOps is not connected for this user.");

        connection.SetOrganization(request.OrganizationName);
        await _connectionRepository.UpdateAsync(connection, cancellationToken);
    }
}
