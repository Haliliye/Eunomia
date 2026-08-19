using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public class CompleteGitHubConnectionCommandHandler : IRequestHandler<CompleteGitHubConnectionCommand, CompleteGitHubConnectionResult>
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ITokenCipher _tokenCipher;
    private readonly IGitHubConnectionRepository _connectionRepository;

    public CompleteGitHubConnectionCommandHandler(IGitHubClient gitHubClient, ITokenCipher tokenCipher, IGitHubConnectionRepository connectionRepository)
    {
        _gitHubClient = gitHubClient;
        _tokenCipher = tokenCipher;
        _connectionRepository = connectionRepository;
    }

    public async Task<CompleteGitHubConnectionResult> Handle(CompleteGitHubConnectionCommand request, CancellationToken cancellationToken)
    {
        string userId;
        try
        {
            var payload = _tokenCipher.Decrypt(request.State);
            userId = GitHubOAuthState.TryUnprotect(payload)
                ?? throw new InvalidOperationException("expired or malformed state");
        }
        catch (Exception)
        {
            return new CompleteGitHubConnectionResult(false, null, "The connection request expired or was invalid. Please try connecting again.");
        }

        try
        {
            var tokenResult = await _gitHubClient.ExchangeCodeForTokenAsync(request.Code, cancellationToken);
            var login = await _gitHubClient.GetAuthenticatedLoginAsync(tokenResult.AccessToken, cancellationToken);

            var accessTokenEncrypted = _tokenCipher.Encrypt(tokenResult.AccessToken);

            var existing = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existing is not null)
            {
                existing.UpdateToken(accessTokenEncrypted, login);
                await _connectionRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var connection = GitHubConnection.Create(Guid.NewGuid().ToString(), userId, accessTokenEncrypted, login);
                await _connectionRepository.AddAsync(connection, cancellationToken);
            }

            return new CompleteGitHubConnectionResult(true, login, null);
        }
        catch (Exception ex)
        {
            return new CompleteGitHubConnectionResult(false, null, $"Couldn't complete the GitHub connection: {ex.Message}");
        }
    }
}
