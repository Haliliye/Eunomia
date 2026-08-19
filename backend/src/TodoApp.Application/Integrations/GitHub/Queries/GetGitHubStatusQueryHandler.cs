using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitHub.Queries;

public class GetGitHubStatusQueryHandler : IRequestHandler<GetGitHubStatusQuery, GitHubStatusDto>
{
    private readonly IGitHubConnectionRepository _connectionRepository;

    public GetGitHubStatusQueryHandler(IGitHubConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task<GitHubStatusDto> Handle(GetGitHubStatusQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken);
        return connection is null
            ? new GitHubStatusDto(false, null, null)
            : new GitHubStatusDto(true, connection.GitHubLogin, connection.ConnectedOn);
    }
}
