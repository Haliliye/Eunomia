using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class ConnectAzureDevOpsCommandHandler : IRequestHandler<ConnectAzureDevOpsCommand, ConnectAzureDevOpsResult>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ITokenCipher _tokenCipher;
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;

    public ConnectAzureDevOpsCommandHandler(IAzureDevOpsClient client, ITokenCipher tokenCipher, IAzureDevOpsConnectionRepository connectionRepository)
    {
        _client = client;
        _tokenCipher = tokenCipher;
        _connectionRepository = connectionRepository;
    }

    public async Task<ConnectAzureDevOpsResult> Handle(ConnectAzureDevOpsCommand request, CancellationToken cancellationToken)
    {
        var organization = request.OrganizationName.Trim();
        var pat = request.PersonalAccessToken.Trim();

        try
        {
            // Fail loudly here (wrong org name, mistyped/expired PAT, PAT
            // missing the needed scopes) rather than storing a token that
            // silently fails on the next import.
            var works = await _client.VerifyAccessAsync(pat, organization, cancellationToken);
            if (!works)
                return new ConnectAzureDevOpsResult(false, "Couldn't access that organization with this token — check the organization name and that the token has Work Items (Read) and Project and Team (Read) scopes.");

            var patEncrypted = _tokenCipher.Encrypt(pat);

            var existing = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken);
            if (existing is not null)
            {
                existing.UpdatePat(organization, patEncrypted);
                await _connectionRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var connection = AzureDevOpsConnection.Create(Guid.NewGuid().ToString(), request.RequestingUserId, organization, patEncrypted);
                await _connectionRepository.AddAsync(connection, cancellationToken);
            }

            return new ConnectAzureDevOpsResult(true, null);
        }
        catch (Exception ex)
        {
            return new ConnectAzureDevOpsResult(false, $"Couldn't connect to Azure DevOps: {ex.Message}");
        }
    }
}
