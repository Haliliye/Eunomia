using MediatR;
using TodoApp.Application.Invitations.DTOs;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Invitations.Queries.GetMyInvitations;

public class GetMyInvitationsQueryHandler : IRequestHandler<GetMyInvitationsQuery, IReadOnlyList<InvitationDto>>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;

    public GetMyInvitationsQueryHandler(IInvitationRepository invitationRepository, ITeamRepository teamRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<InvitationDto>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetPendingByUserIdAsync(request.UserId, cancellationToken);

        var result = new List<InvitationDto>();
        foreach (var invitation in invitations)
        {
            var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken);
            result.Add(new InvitationDto(invitation.Id, invitation.TeamId, team?.Name ?? "Unknown team", invitation.InvitedUserId, invitation.InvitedByUserId, invitation.CreatedOn));
        }

        return result;
    }
}
