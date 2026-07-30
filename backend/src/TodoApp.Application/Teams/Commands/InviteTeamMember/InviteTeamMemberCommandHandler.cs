using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Teams.Commands.InviteTeamMember;

public class InviteTeamMemberCommandHandler : IRequestHandler<InviteTeamMemberCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IMediator _mediator;

    public InviteTeamMemberCommandHandler(
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        IInvitationRepository invitationRepository,
        IMediator mediator)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _mediator = mediator;
    }

    public async Task Handle(InviteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        var isOwner = team.Members.Any(m => m.UserId == request.InvitingUserId && m.Role == TeamRole.Owner);
        if (!isOwner)
            throw new UnauthorizedAccessException("Only the team owner can invite members.");

        // US-104 AC: "enter an identifier that doesn't exist -> clear error message" —
        // only a person with a real account can be invited.
        var invitedUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new KeyNotFoundException($"No account found for \"{request.Email}\". They need to create an account first.");

        if (team.Members.Any(m => m.UserId == invitedUser.Id))
            throw new InvalidOperationException($"{request.Email} is already a member of this team.");

        if (await _invitationRepository.HasPendingInvitationAsync(team.Id, invitedUser.Id, cancellationToken))
            throw new InvalidOperationException($"{request.Email} already has a pending invitation to this team.");

        var invitation = Invitation.Create(
            id: Guid.NewGuid().ToString(),
            teamId: team.Id,
            invitedUserId: invitedUser.Id,
            invitedByUserId: request.InvitingUserId);

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        // InvitationCreatedEvent -> InvitationCreatedEventHandler creates the
        // notification (with accept/decline actions) and pushes it live —
        // this handler doesn't touch Team.Members at all; that only happens
        // if/when the invitation is accepted.
        await _mediator.PublishDomainEventsAsync(invitation, cancellationToken);
    }
}
