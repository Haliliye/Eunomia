using TodoApp.Domain.Common;

namespace TodoApp.Domain.Invitations;

public sealed class InvitationCreatedEvent : IDomainEvent
{
    public string InvitationId { get; }
    public string TeamId { get; }
    public string InvitedUserId { get; }
    public string InvitedByUserId { get; }
    public DateTime OccurredOn { get; }

    public InvitationCreatedEvent(string invitationId, string teamId, string invitedUserId, string invitedByUserId)
    {
        InvitationId = invitationId;
        TeamId = teamId;
        InvitedUserId = invitedUserId;
        InvitedByUserId = invitedByUserId;
        OccurredOn = DateTime.UtcNow;
    }
}
