namespace TodoApp.Infrastructure.Persistence.Documents;

public class EmailSignupInvitationDocument
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string InvitedByUserId { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}
