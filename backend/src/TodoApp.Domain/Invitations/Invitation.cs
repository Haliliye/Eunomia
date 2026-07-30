using TodoApp.Domain.Common;

namespace TodoApp.Domain.Invitations;

/// <summary>
/// A pending "join this team" invitation (US-104's accept/decline semantics).
/// Its own aggregate rather than embedded in Team, since a person can have
/// invitations to teams they aren't a member of yet, and querying "my pending
/// invitations" shouldn't require scanning every team.
/// </summary>
public class Invitation : AggregateRoot
{
    public string TeamId { get; private set; } = string.Empty;
    public string InvitedUserId { get; private set; } = string.Empty;
    public string InvitedByUserId { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
    public DateTime CreatedOn { get; private set; }
    public DateTime? RespondedOn { get; private set; }

    private Invitation() { }

    private Invitation(string id, string teamId, string invitedUserId, string invitedByUserId) : base(id)
    {
        TeamId = teamId;
        InvitedUserId = invitedUserId;
        InvitedByUserId = invitedByUserId;
        CreatedOn = DateTime.UtcNow;
    }

    public static Invitation Create(string id, string teamId, string invitedUserId, string invitedByUserId)
    {
        var invitation = new Invitation(id, teamId, invitedUserId, invitedByUserId);
        invitation.RaiseDomainEvent(new InvitationCreatedEvent(invitation.Id, teamId, invitedUserId, invitedByUserId));
        return invitation;
    }

    public static Invitation Rehydrate(
        string id, string teamId, string invitedUserId, string invitedByUserId,
        InvitationStatus status, DateTime createdOn, DateTime? respondedOn)
    {
        var invitation = new Invitation(id, teamId, invitedUserId, invitedByUserId)
        {
            Status = status,
            CreatedOn = createdOn,
            RespondedOn = respondedOn
        };
        return invitation;
    }

    public void Accept(string respondingUserId)
    {
        EnsureRespondingUserIsInvitee(respondingUserId);
        EnsurePending();
        Status = InvitationStatus.Accepted;
        RespondedOn = DateTime.UtcNow;
    }

    public void Decline(string respondingUserId)
    {
        EnsureRespondingUserIsInvitee(respondingUserId);
        EnsurePending();
        Status = InvitationStatus.Declined;
        RespondedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Withdraws a still-pending invitation — called by the inviter (or, per
    /// the handler, the team owner) before the invitee has responded.
    /// </summary>
    public void Cancel()
    {
        EnsurePending();
        Status = InvitationStatus.Cancelled;
        RespondedOn = DateTime.UtcNow;
    }

    private void EnsureRespondingUserIsInvitee(string respondingUserId)
    {
        if (respondingUserId != InvitedUserId)
            throw new UnauthorizedAccessException("Only the invited person can respond to this invitation.");
    }

    private void EnsurePending()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"This invitation was already {Status.ToString().ToLowerInvariant()}.");
    }
}
