using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.Jira;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class CompleteJiraConnectionCommandHandler : IRequestHandler<CompleteJiraConnectionCommand, CompleteJiraConnectionResult>
{
    private readonly IJiraClient _jiraClient;
    private readonly ITokenCipher _tokenCipher;
    private readonly IJiraConnectionRepository _connectionRepository;

    public CompleteJiraConnectionCommandHandler(IJiraClient jiraClient, ITokenCipher tokenCipher, IJiraConnectionRepository connectionRepository)
    {
        _jiraClient = jiraClient;
        _tokenCipher = tokenCipher;
        _connectionRepository = connectionRepository;
    }

    public async Task<CompleteJiraConnectionResult> Handle(CompleteJiraConnectionCommand request, CancellationToken cancellationToken)
    {
        string userId;
        try
        {
            var payload = _tokenCipher.Decrypt(request.State);
            userId = JiraOAuthState.TryUnprotect(payload)
                ?? throw new InvalidOperationException("expired or malformed state");
        }
        catch (Exception)
        {
            // Wrong/tampered/expired state — most likely the user took too
            // long on Atlassian's consent screen, or this is a replayed URL.
            return new CompleteJiraConnectionResult(false, null, "The connection request expired or was invalid. Please try connecting again.");
        }

        try
        {
            var tokenResult = await _jiraClient.ExchangeCodeForTokenAsync(request.Code, cancellationToken);
            var resources = await _jiraClient.GetAccessibleResourcesAsync(tokenResult.AccessToken, cancellationToken);

            var site = resources.FirstOrDefault();
            if (site is null)
                return new CompleteJiraConnectionResult(false, null, "No accessible Jira site was found for this account.");

            var accessTokenEncrypted = _tokenCipher.Encrypt(tokenResult.AccessToken);
            var refreshTokenEncrypted = _tokenCipher.Encrypt(tokenResult.RefreshToken);

            var existing = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existing is not null)
            {
                // Re-connecting (e.g. switching Jira accounts, or the refresh
                // token was revoked on Atlassian's side) — replace in place
                // rather than creating a second row for the same user.
                existing.UpdateTokens(accessTokenEncrypted, refreshTokenEncrypted, tokenResult.ExpiresOn);
                await _connectionRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var connection = JiraConnection.Create(
                    Guid.NewGuid().ToString(), userId, site.CloudId, site.Url, site.Name,
                    accessTokenEncrypted, refreshTokenEncrypted, tokenResult.ExpiresOn);
                await _connectionRepository.AddAsync(connection, cancellationToken);
            }

            return new CompleteJiraConnectionResult(true, site.Name, null);
        }
        catch (Exception ex)
        {
            return new CompleteJiraConnectionResult(false, null, $"Couldn't complete the Jira connection: {ex.Message}");
        }
    }
}
