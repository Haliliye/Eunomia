using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public class StartGitHubConnectionCommandHandler : IRequestHandler<StartGitHubConnectionCommand, string>
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ITokenCipher _tokenCipher;

    public StartGitHubConnectionCommandHandler(IGitHubClient gitHubClient, ITokenCipher tokenCipher)
    {
        _gitHubClient = gitHubClient;
        _tokenCipher = tokenCipher;
    }

    public Task<string> Handle(StartGitHubConnectionCommand request, CancellationToken cancellationToken)
    {
        var state = _tokenCipher.Encrypt(GitHubOAuthState.Protect(request.RequestingUserId));
        var authorizationUrl = _gitHubClient.BuildAuthorizationUrl(state);
        return Task.FromResult(authorizationUrl);
    }
}
