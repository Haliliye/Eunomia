namespace TodoApp.Infrastructure.Persistence.Documents;

public class BoardDocument
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SprintId { get; set; }
    public DateTime CreatedOn { get; set; }
}
