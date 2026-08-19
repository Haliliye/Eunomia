using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitHub;

/// <summary>
/// Much simpler than JiraAccessTokenProvider: GitHub's classic OAuth App
/// tokens don't expire, so there's no refresh dance to hide from callers —
/// this just decrypts the stored token. Kept as its own small class anyway
/// so every handler that needs the token follows the same "look it up
/// through a provider, not the raw repository" shape as Jira/Azure DevOps,
/// and so a refresh story can be added here later without touching callers
/// if GitHub Apps (which do expire tokens) are adopted instead someday.
/// </summary>
public class GitHubAccessTokenProvider
{
    private readonly IGitHubConnectionRepository _connectionRepository;
    private readonly ITokenCipher _tokenCipher;

    public GitHubAccessTokenProvider(IGitHubConnectionRepository connectionRepository, ITokenCipher tokenCipher)
    {
        _connectionRepository = connectionRepository;
        _tokenCipher = tokenCipher;
    }

    public async Task<(GitHubConnection Connection, string AccessToken)> GetAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("GitHub is not connected for this user.");

        return (connection, _tokenCipher.Decrypt(connection.AccessTokenEncrypted));
    }
}
