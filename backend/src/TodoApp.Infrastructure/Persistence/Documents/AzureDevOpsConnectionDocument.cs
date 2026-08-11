namespace TodoApp.Infrastructure.Persistence.Documents;

public class AzureDevOpsConnectionDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public string RefreshTokenEncrypted { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresOn { get; set; }
    public DateTime ConnectedOn { get; set; }
}
