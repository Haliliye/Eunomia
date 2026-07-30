using MediatR;

namespace TodoApp.Application.Teams.Commands.InviteTeamMember;

/// <summary>Sends a pending invitation by email — does not add the person to the team until they accept (see AcceptInvitationCommand).</summary>
public record InviteTeamMemberCommand(string TeamId, string Email, string InvitingUserId) : IRequest;
