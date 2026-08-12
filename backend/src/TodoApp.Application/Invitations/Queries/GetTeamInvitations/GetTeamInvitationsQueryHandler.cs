using MediatR;
using TodoApp.Application.Invitations.DTOs;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Invitations.Queries.GetTeamInvitations;

public class GetTeamInvitationsQueryHandler : IRequestHandler<GetTeamInvitationsQuery, IReadOnlyList<InvitationDto>>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamInvitationsQueryHandler(IInvitationRepository invitationRepository, ITeamRepository teamRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<InvitationDto>> Handle(GetTeamInvitationsQuery request, CancellationToken cancellationToken)
    {
        // Previously missing entirely — any authenticated user could list a
        // team's pending invitations (who's been invited, by whom) just by
        // knowing/guessing its id. Same permission level as sending an
        // invite in the first place.
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var invitations = await _invitationRepository.GetPendingByTeamIdAsync(request.TeamId, cancellationToken);

        // TeamName is redundant here (caller already knows which team), but
        // keeping InvitationDto's shape consistent with GetMyInvitationsQuery
        // avoids a second, near-identical DTO.
        return invitations
            .Select(i => new InvitationDto(i.Id, i.TeamId, string.Empty, i.InvitedUserId, i.InvitedByUserId, i.CreatedOn))
            .ToList();
    }
}
