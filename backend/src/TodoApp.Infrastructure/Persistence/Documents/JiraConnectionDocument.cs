namespace TodoApp.Infrastructure.Persistence.Documents;

public class JiraConnectionDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CloudId { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public string RefreshTokenEncrypted { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresOn { get; set; }
    public DateTime ConnectedOn { get; set; }
}
