using TodoApp.Domain.Common;

namespace TodoApp.Domain.Integrations;

/// <summary>
/// One user's link to their Azure DevOps organization, via a Personal
/// Access Token (PAT) rather than OAuth. This wasn't the original plan —
/// OAuth was tried first, mirroring JiraConnection — but Microsoft's Entra
/// ID OAuth apps don't support personal Microsoft accounts for the Azure
/// DevOps resource, and Azure DevOps' own classic OAuth app registration has
/// since been fully discontinued ("OAuth App registration is no longer
/// available"), leaving PAT as the only viable option for an account that
/// isn't a work/school (Entra ID) identity. The PAT is stored encrypted, see
/// ITokenCipher — same as Jira/OAuth tokens, just without a refresh
/// flow (a PAT's own expiry, set by the person when they create it in Azure
/// DevOps, is what eventually invalidates this — there's no API-driven
/// renewal for PATs).
/// </summary>
public class AzureDevOpsConnection : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string OrganizationName { get; private set; } = string.Empty;
    public string PersonalAccessTokenEncrypted { get; private set; } = string.Empty;
    public DateTime ConnectedOn { get; private set; }

    private AzureDevOpsConnection() { }

    private AzureDevOpsConnection(string id, string userId, string organizationName, string personalAccessTokenEncrypted) : base(id)
    {
        UserId = userId;
        OrganizationName = organizationName;
        PersonalAccessTokenEncrypted = personalAccessTokenEncrypted;
        ConnectedOn = DateTime.UtcNow;
    }

    public static AzureDevOpsConnection Create(string id, string userId, string organizationName, string personalAccessTokenEncrypted) =>
        new(id, userId, organizationName, personalAccessTokenEncrypted);

    public static AzureDevOpsConnection Rehydrate(string id, string userId, string organizationName, string personalAccessTokenEncrypted, DateTime connectedOn)
    {
        var connection = new AzureDevOpsConnection(id, userId, organizationName, personalAccessTokenEncrypted) { ConnectedOn = connectedOn };
        return connection;
    }

    public void UpdatePat(string organizationName, string personalAccessTokenEncrypted)
    {
        OrganizationName = organizationName;
        PersonalAccessTokenEncrypted = personalAccessTokenEncrypted;
    }
}
