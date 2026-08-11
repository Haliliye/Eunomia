namespace TodoApp.Infrastructure.Persistence.Documents;

public class AzureDevOpsConnectionDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string PersonalAccessTokenEncrypted { get; set; } = string.Empty;
    public DateTime ConnectedOn { get; set; }
}
