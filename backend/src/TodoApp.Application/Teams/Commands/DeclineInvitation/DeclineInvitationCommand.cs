using MediatR;

namespace TodoApp.Application.Teams.Commands.DeclineInvitation;

public record DeclineInvitationCommand(string InvitationId, string RespondingUserId) : IRequest;
