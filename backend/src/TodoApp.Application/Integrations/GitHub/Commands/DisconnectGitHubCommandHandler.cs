using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public class DisconnectGitHubCommandHandler : IRequestHandler<DisconnectGitHubCommand>
{
    private readonly IGitHubConnectionRepository _connectionRepository;

    public DisconnectGitHubCommandHandler(IGitHubConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task Handle(DisconnectGitHubCommand request, CancellationToken cancellationToken) =>
        await _connectionRepository.DeleteByUserIdAsync(request.RequestingUserId, cancellationToken);
}
