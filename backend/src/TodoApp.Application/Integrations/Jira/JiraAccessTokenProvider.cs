using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.Jira;

/// <summary>
/// Shared by every handler that needs to actually call the Jira API
/// (GetJiraProjectsQuery, PreviewJiraImportQuery, ImportFromJiraCommand) —
/// transparently refreshes the access token when it's expired (or close to
/// it) and persists the new (rotating) token pair, so callers never have to
/// think about token lifetime themselves.
/// </summary>
public class JiraAccessTokenProvider
{
    private readonly IJiraConnectionRepository _connectionRepository;
    private readonly IJiraClient _jiraClient;
    private readonly ITokenCipher _tokenCipher;

    public JiraAccessTokenProvider(IJiraConnectionRepository connectionRepository, IJiraClient jiraClient, ITokenCipher tokenCipher)
    {
        _connectionRepository = connectionRepository;
        _jiraClient = jiraClient;
        _tokenCipher = tokenCipher;
    }

    public async Task<(JiraConnection Connection, string AccessToken)> GetValidAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Jira is not connected for this user.");

        if (!connection.AccessTokenNeedsRefresh)
            return (connection, _tokenCipher.Decrypt(connection.AccessTokenEncrypted));

        var refreshToken = _tokenCipher.Decrypt(connection.RefreshTokenEncrypted);
        var tokenResult = await _jiraClient.RefreshTokenAsync(refreshToken, cancellationToken);

        connection.UpdateTokens(
            _tokenCipher.Encrypt(tokenResult.AccessToken),
            _tokenCipher.Encrypt(tokenResult.RefreshToken),
            tokenResult.ExpiresOn);
        await _connectionRepository.UpdateAsync(connection, cancellationToken);

        return (connection, tokenResult.AccessToken);
    }
}
