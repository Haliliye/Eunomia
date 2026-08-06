using TodoApp.Domain.Common;

namespace TodoApp.Domain.Invitations;

/// <summary>
/// A "come sign up for Eunomia" invitation sent by email to someone who
/// isn't a Eunomia user yet (e.g. a Jira assignee found during an import —
/// see ImportFromJiraCommandHandler). Distinct from Invitation, which
/// targets an existing InvitedUserId: this one only has an email address to
/// go on, since the person doesn't have an account (or a User.Id) to invite
/// yet.
///
/// Fulfilled automatically by RegisterCommandHandler: when someone
/// registers with a matching email, every pending EmailSignupInvitation for
/// that email adds them to the corresponding team and is then deleted.
/// </summary>
public class EmailSignupInvitation : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string TeamId { get; private set; } = string.Empty;
    public string InvitedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedOn { get; private set; }

    private EmailSignupInvitation() { }

    private EmailSignupInvitation(string id, string email, string teamId, string invitedByUserId) : base(id)
    {
        Email = email.Trim().ToLowerInvariant();
        TeamId = teamId;
        InvitedByUserId = invitedByUserId;
        CreatedOn = DateTime.UtcNow;
    }

    public static EmailSignupInvitation Create(string id, string email, string teamId, string invitedByUserId) =>
        new(id, email, teamId, invitedByUserId);

    public static EmailSignupInvitation Rehydrate(string id, string email, string teamId, string invitedByUserId, DateTime createdOn)
    {
        var invitation = new EmailSignupInvitation(id, email, teamId, invitedByUserId) { CreatedOn = createdOn };
        return invitation;
    }
}
