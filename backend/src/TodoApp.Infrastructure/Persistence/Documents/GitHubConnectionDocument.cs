namespace TodoApp.Infrastructure.Persistence.Documents;

public class GitHubConnectionDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public string? GitHubLogin { get; set; }
    public DateTime ConnectedOn { get; set; }
}
