using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.GitLab.Queries;

public class GetGitLabProjectsQueryHandler : IRequestHandler<GetGitLabProjectsQuery, IReadOnlyList<GitLabProjectDto>>
{
    private readonly GitLabAccessTokenProvider _accessTokenProvider;
    private readonly IGitLabClient _gitLabClient;

    public GetGitLabProjectsQueryHandler(GitLabAccessTokenProvider accessTokenProvider, IGitLabClient gitLabClient)
    {
        _accessTokenProvider = accessTokenProvider;
        _gitLabClient = gitLabClient;
    }

    public async Task<IReadOnlyList<GitLabProjectDto>> Handle(GetGitLabProjectsQuery request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        return await _gitLabClient.GetProjectsAsync(accessToken, cancellationToken);
    }
}
