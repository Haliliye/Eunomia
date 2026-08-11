using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps;

/// <summary>Mirrors JiraAccessTokenProvider — transparently refreshes an expired access token and persists the new pair.</summary>
public class AzureDevOpsAccessTokenProvider
{
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;
    private readonly IAzureDevOpsClient _client;
    private readonly ITokenCipher _tokenCipher;

    public AzureDevOpsAccessTokenProvider(IAzureDevOpsConnectionRepository connectionRepository, IAzureDevOpsClient client, ITokenCipher tokenCipher)
    {
        _connectionRepository = connectionRepository;
        _client = client;
        _tokenCipher = tokenCipher;
    }

    public async Task<(AzureDevOpsConnection Connection, string AccessToken)> GetValidAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Azure DevOps is not connected for this user.");

        if (!connection.AccessTokenNeedsRefresh)
            return (connection, _tokenCipher.Decrypt(connection.AccessTokenEncrypted));

        if (string.IsNullOrEmpty(connection.RefreshTokenEncrypted))
            throw new InvalidOperationException("Your Azure DevOps connection has expired. Please reconnect.");

        var refreshToken = _tokenCipher.Decrypt(connection.RefreshTokenEncrypted);
        var tokenResult = await _client.RefreshTokenAsync(refreshToken, cancellationToken);

        connection.UpdateTokens(
            _tokenCipher.Encrypt(tokenResult.AccessToken),
            _tokenCipher.Encrypt(tokenResult.RefreshToken),
            tokenResult.ExpiresOn);
        await _connectionRepository.UpdateAsync(connection, cancellationToken);

        return (connection, tokenResult.AccessToken);
    }
}
