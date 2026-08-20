using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public class DisconnectGitLabCommandHandler : IRequestHandler<DisconnectGitLabCommand>
{
    private readonly IGitLabConnectionRepository _connectionRepository;

    public DisconnectGitLabCommandHandler(IGitLabConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task Handle(DisconnectGitLabCommand request, CancellationToken cancellationToken) =>
        await _connectionRepository.DeleteByUserIdAsync(request.RequestingUserId, cancellationToken);
}
