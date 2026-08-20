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
        // IGitHubClient is registered in DI unconditionally now (see
        // DependencyInjection.cs for why), so this is the actual "GitHub
        // isn't set up on this server" check — surfaced as a normal
        // per-request error here instead of crashing the whole app at startup.
        if (!_gitHubClient.IsConfigured)
            throw new InvalidOperationException("GitHub integration isn't configured on this server yet.");

        var state = _tokenCipher.Encrypt(GitHubOAuthState.Protect(request.RequestingUserId));
        var authorizationUrl = _gitHubClient.BuildAuthorizationUrl(state);
        return Task.FromResult(authorizationUrl);
    }
}
