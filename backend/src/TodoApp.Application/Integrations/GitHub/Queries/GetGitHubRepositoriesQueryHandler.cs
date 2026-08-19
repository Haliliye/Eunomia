using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.GitHub;

namespace TodoApp.Application.Integrations.GitHub.Queries;

public class GetGitHubRepositoriesQueryHandler : IRequestHandler<GetGitHubRepositoriesQuery, IReadOnlyList<GitHubRepositoryDto>>
{
    private readonly GitHubAccessTokenProvider _accessTokenProvider;
    private readonly IGitHubClient _gitHubClient;

    public GetGitHubRepositoriesQueryHandler(GitHubAccessTokenProvider accessTokenProvider, IGitHubClient gitHubClient)
    {
        _accessTokenProvider = accessTokenProvider;
        _gitHubClient = gitHubClient;
    }

    public async Task<IReadOnlyList<GitHubRepositoryDto>> Handle(GetGitHubRepositoriesQuery request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await _accessTokenProvider.GetAccessTokenAsync(request.RequestingUserId, cancellationToken);
        return await _gitHubClient.GetRepositoriesAsync(accessToken, cancellationToken);
    }
}
