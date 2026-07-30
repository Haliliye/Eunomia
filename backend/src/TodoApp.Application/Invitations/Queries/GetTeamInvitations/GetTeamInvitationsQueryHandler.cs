using MediatR;
using TodoApp.Application.Invitations.DTOs;
using TodoApp.Domain.Invitations;

namespace TodoApp.Application.Invitations.Queries.GetTeamInvitations;

public class GetTeamInvitationsQueryHandler : IRequestHandler<GetTeamInvitationsQuery, IReadOnlyList<InvitationDto>>
{
    private readonly IInvitationRepository _invitationRepository;

    public GetTeamInvitationsQueryHandler(IInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<IReadOnlyList<InvitationDto>> Handle(GetTeamInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetPendingByTeamIdAsync(request.TeamId, cancellationToken);

        // TeamName is redundant here (caller already knows which team), but
        // keeping InvitationDto's shape consistent with GetMyInvitationsQuery
        // avoids a second, near-identical DTO.
        return invitations
            .Select(i => new InvitationDto(i.Id, i.TeamId, string.Empty, i.InvitedUserId, i.InvitedByUserId, i.CreatedOn))
            .ToList();
    }
}
