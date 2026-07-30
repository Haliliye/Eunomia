using MediatR;

namespace TodoApp.Application.Teams.Commands.CancelInvitation;

public record CancelInvitationCommand(string InvitationId, string RequestingUserId) : IRequest;
