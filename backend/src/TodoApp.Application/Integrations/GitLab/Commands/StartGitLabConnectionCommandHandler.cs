using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public class StartGitLabConnectionCommandHandler : IRequestHandler<StartGitLabConnectionCommand, string>
{
    private readonly IGitLabClient _gitLabClient;
    private readonly ITokenCipher _tokenCipher;

    public StartGitLabConnectionCommandHandler(IGitLabClient gitLabClient, ITokenCipher tokenCipher)
    {
        _gitLabClient = gitLabClient;
        _tokenCipher = tokenCipher;
    }

    public Task<string> Handle(StartGitLabConnectionCommand request, CancellationToken cancellationToken)
    {
        // IGitLabClient is registered in DI unconditionally now (see
        // DependencyInjection.cs for why), so this is the actual "GitLab
        // isn't set up on this server" check — surfaced as a normal
        // per-request error here instead of crashing the whole app at startup.
        if (!_gitLabClient.IsConfigured)
            throw new InvalidOperationException("GitLab integration isn't configured on this server yet.");

        var state = _tokenCipher.Encrypt(GitLabOAuthState.Protect(request.RequestingUserId));
        var authorizationUrl = _gitLabClient.BuildAuthorizationUrl(state);
        return Task.FromResult(authorizationUrl);
    }
}
