using MediatR;
using TodoApp.Application.Invitations.DTOs;

namespace TodoApp.Application.Invitations.Queries.GetMyInvitations;

public record GetMyInvitationsQuery(string UserId) : IRequest<IReadOnlyList<InvitationDto>>;
