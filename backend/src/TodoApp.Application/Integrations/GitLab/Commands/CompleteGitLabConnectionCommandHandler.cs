using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public class CompleteGitLabConnectionCommandHandler : IRequestHandler<CompleteGitLabConnectionCommand, CompleteGitLabConnectionResult>
{
    private readonly IGitLabClient _gitLabClient;
    private readonly ITokenCipher _tokenCipher;
    private readonly IGitLabConnectionRepository _connectionRepository;

    public CompleteGitLabConnectionCommandHandler(IGitLabClient gitLabClient, ITokenCipher tokenCipher, IGitLabConnectionRepository connectionRepository)
    {
        _gitLabClient = gitLabClient;
        _tokenCipher = tokenCipher;
        _connectionRepository = connectionRepository;
    }

    public async Task<CompleteGitLabConnectionResult> Handle(CompleteGitLabConnectionCommand request, CancellationToken cancellationToken)
    {
        string userId;
        try
        {
            var payload = _tokenCipher.Decrypt(request.State);
            userId = GitLabOAuthState.TryUnprotect(payload)
                ?? throw new InvalidOperationException("expired or malformed state");
        }
        catch (Exception)
        {
            return new CompleteGitLabConnectionResult(false, null, "The connection request expired or was invalid. Please try connecting again.");
        }

        try
        {
            var tokenResult = await _gitLabClient.ExchangeCodeForTokenAsync(request.Code, cancellationToken);
            var username = await _gitLabClient.GetAuthenticatedUsernameAsync(tokenResult.AccessToken, cancellationToken);

            var accessTokenEncrypted = _tokenCipher.Encrypt(tokenResult.AccessToken);
            var refreshTokenEncrypted = _tokenCipher.Encrypt(tokenResult.RefreshToken);

            var existing = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existing is not null)
            {
                existing.UpdateTokens(accessTokenEncrypted, refreshTokenEncrypted, tokenResult.ExpiresOn, username);
                await _connectionRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var connection = GitLabConnection.Create(Guid.NewGuid().ToString(), userId, username, accessTokenEncrypted, refreshTokenEncrypted, tokenResult.ExpiresOn);
                await _connectionRepository.AddAsync(connection, cancellationToken);
            }

            return new CompleteGitLabConnectionResult(true, username, null);
        }
        catch (Exception ex)
        {
            return new CompleteGitLabConnectionResult(false, null, $"Couldn't complete the GitLab connection: {ex.Message}");
        }
    }
}
