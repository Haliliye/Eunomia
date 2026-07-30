using MediatR;
using TodoApp.Application.Invitations.DTOs;

namespace TodoApp.Application.Invitations.Queries.GetTeamInvitations;

public record GetTeamInvitationsQuery(string TeamId) : IRequest<IReadOnlyList<InvitationDto>>;
