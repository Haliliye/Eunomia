using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitLab;

/// <summary>
/// Same shape as JiraAccessTokenProvider (not GitHubAccessTokenProvider's
/// simpler version) — GitLab's OAuth tokens expire and need refreshing.
/// Transparently refreshes when needed and persists the new (rotating)
/// token pair, so callers never have to think about token lifetime.
/// </summary>
public class GitLabAccessTokenProvider
{
    private readonly IGitLabConnectionRepository _connectionRepository;
    private readonly IGitLabClient _gitLabClient;
    private readonly ITokenCipher _tokenCipher;

    public GitLabAccessTokenProvider(IGitLabConnectionRepository connectionRepository, IGitLabClient gitLabClient, ITokenCipher tokenCipher)
    {
        _connectionRepository = connectionRepository;
        _gitLabClient = gitLabClient;
        _tokenCipher = tokenCipher;
    }

    public async Task<(GitLabConnection Connection, string AccessToken)> GetValidAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("GitLab is not connected for this user.");

        if (!connection.AccessTokenNeedsRefresh)
            return (connection, _tokenCipher.Decrypt(connection.AccessTokenEncrypted));

        var refreshToken = _tokenCipher.Decrypt(connection.RefreshTokenEncrypted);
        var tokenResult = await _gitLabClient.RefreshTokenAsync(refreshToken, cancellationToken);

        connection.UpdateTokens(
            _tokenCipher.Encrypt(tokenResult.AccessToken),
            _tokenCipher.Encrypt(tokenResult.RefreshToken),
            tokenResult.ExpiresOn,
            gitLabUsername: null); // unchanged — a refresh doesn't return profile info
        await _connectionRepository.UpdateAsync(connection, cancellationToken);

        return (connection, tokenResult.AccessToken);
    }
}
