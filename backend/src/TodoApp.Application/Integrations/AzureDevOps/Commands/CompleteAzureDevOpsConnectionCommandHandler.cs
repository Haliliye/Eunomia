using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.AzureDevOps;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class CompleteAzureDevOpsConnectionCommandHandler : IRequestHandler<CompleteAzureDevOpsConnectionCommand, CompleteAzureDevOpsConnectionResult>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ITokenCipher _tokenCipher;
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;

    public CompleteAzureDevOpsConnectionCommandHandler(IAzureDevOpsClient client, ITokenCipher tokenCipher, IAzureDevOpsConnectionRepository connectionRepository)
    {
        _client = client;
        _tokenCipher = tokenCipher;
        _connectionRepository = connectionRepository;
    }

    public async Task<CompleteAzureDevOpsConnectionResult> Handle(CompleteAzureDevOpsConnectionCommand request, CancellationToken cancellationToken)
    {
        string userId;
        try
        {
            var payload = _tokenCipher.Decrypt(request.State);
            userId = AzureDevOpsOAuthState.TryUnprotect(payload)
                ?? throw new InvalidOperationException("expired or malformed state");
        }
        catch (Exception)
        {
            return new CompleteAzureDevOpsConnectionResult(false, "The connection request expired or was invalid. Please try connecting again.");
        }

        try
        {
            var tokenResult = await _client.ExchangeCodeForTokenAsync(request.Code, cancellationToken);

            var accessTokenEncrypted = _tokenCipher.Encrypt(tokenResult.AccessToken);
            var refreshTokenEncrypted = _tokenCipher.Encrypt(tokenResult.RefreshToken);

            var existing = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existing is not null)
            {
                existing.UpdateTokens(accessTokenEncrypted, refreshTokenEncrypted, tokenResult.ExpiresOn);
                await _connectionRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var connection = AzureDevOpsConnection.Create(
                    Guid.NewGuid().ToString(), userId, accessTokenEncrypted, refreshTokenEncrypted, tokenResult.ExpiresOn);
                await _connectionRepository.AddAsync(connection, cancellationToken);
            }

            return new CompleteAzureDevOpsConnectionResult(true, null);
        }
        catch (Exception ex)
        {
            return new CompleteAzureDevOpsConnectionResult(false, $"Couldn't complete the Azure DevOps connection: {ex.Message}");
        }
    }
}
