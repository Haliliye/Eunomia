using MediatR;

namespace TodoApp.Application.Teams.Commands.AcceptInvitation;

public record AcceptInvitationCommand(string InvitationId, string RespondingUserId) : IRequest;
