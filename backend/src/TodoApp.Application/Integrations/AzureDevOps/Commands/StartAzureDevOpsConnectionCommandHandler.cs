using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.AzureDevOps;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class StartAzureDevOpsConnectionCommandHandler : IRequestHandler<StartAzureDevOpsConnectionCommand, string>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ITokenCipher _tokenCipher;

    public StartAzureDevOpsConnectionCommandHandler(IAzureDevOpsClient client, ITokenCipher tokenCipher)
    {
        _client = client;
        _tokenCipher = tokenCipher;
    }

    public Task<string> Handle(StartAzureDevOpsConnectionCommand request, CancellationToken cancellationToken)
    {
        var state = _tokenCipher.Encrypt(AzureDevOpsOAuthState.Protect(request.RequestingUserId));
        var authorizationUrl = _client.BuildAuthorizationUrl(state); // BuildAuthorizationUrl URL-encodes the state itself — don't double-encode here
        return Task.FromResult(authorizationUrl);
    }
}
