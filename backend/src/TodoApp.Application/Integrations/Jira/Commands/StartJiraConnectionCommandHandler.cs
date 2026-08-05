using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.Jira;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class StartJiraConnectionCommandHandler : IRequestHandler<StartJiraConnectionCommand, string>
{
    private readonly IJiraClient _jiraClient;
    private readonly ITokenCipher _tokenCipher;

    public StartJiraConnectionCommandHandler(IJiraClient jiraClient, ITokenCipher tokenCipher)
    {
        _jiraClient = jiraClient;
        _tokenCipher = tokenCipher;
    }

    public Task<string> Handle(StartJiraConnectionCommand request, CancellationToken cancellationToken)
    {
        // The OAuth "state" round-trips through Atlassian and the user's
        // browser untouched, so it's the only way to carry our own context
        // (which user started this) into the callback below — the callback
        // is a plain browser GET with no JWT. Encrypting it (rather than
        // just base64) means it can't be read or forged by anyone
        // intercepting the redirect, and JiraOAuthState.TryUnprotect rejects
        // it once it's more than a few minutes old.
        var state = _tokenCipher.Encrypt(JiraOAuthState.Protect(request.RequestingUserId));
        var authorizationUrl = _jiraClient.BuildAuthorizationUrl(state); // BuildAuthorizationUrl URL-encodes the state itself — don't double-encode here
        return Task.FromResult(authorizationUrl);
    }
}
