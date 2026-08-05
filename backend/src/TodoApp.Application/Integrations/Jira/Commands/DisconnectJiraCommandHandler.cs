using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class DisconnectJiraCommandHandler : IRequestHandler<DisconnectJiraCommand>
{
    private readonly IJiraConnectionRepository _connectionRepository;

    public DisconnectJiraCommandHandler(IJiraConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task Handle(DisconnectJiraCommand request, CancellationToken cancellationToken) =>
        await _connectionRepository.DeleteByUserIdAsync(request.RequestingUserId, cancellationToken);
}
