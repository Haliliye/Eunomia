using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.CreateTeam;

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, TeamDto>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IMediator _mediator;

    public CreateTeamCommandHandler(ITeamRepository teamRepository, IMediator mediator)
    {
        _teamRepository = teamRepository;
        _mediator = mediator;
    }

    public async Task<TeamDto> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await _teamRepository.ExistsWithNameForUserAsync(
            request.Name, request.OwnerId, cancellationToken);

        if (nameTaken)
            throw new InvalidOperationException("You already have a team with this name.");

        var team = Team.Create(
            id: Guid.NewGuid().ToString(),
            name: request.Name,
            description: request.Description,
            ownerId: request.OwnerId);

        await _teamRepository.AddAsync(team, cancellationToken);

        // No subscriber for TeamCreatedEvent yet (no welcome-email/audit-log
        // feature exists), but dispatching it here means one can be added
        // later (a new INotificationHandler<...>) without touching this
        // handler again — see DomainEventDispatchExtensions.
        await _mediator.PublishDomainEventsAsync(team, cancellationToken);

        return TeamMapper.ToDto(team);
    }
}
