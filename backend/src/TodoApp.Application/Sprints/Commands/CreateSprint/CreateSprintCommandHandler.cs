using MediatR;
using TodoApp.Application.Sprints.DTOs;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Sprints.Commands.CreateSprint;

public class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, SprintDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ITeamRepository _teamRepository;

    public CreateSprintCommandHandler(ISprintRepository sprintRepository, ITeamRepository teamRepository)
    {
        _sprintRepository = sprintRepository;
        _teamRepository = teamRepository;
    }

    public async Task<SprintDto> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // Sprint planning is an administrative action — restrict it to
        // owners/admins rather than any member (unlike day-to-day story work).
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var sprint = Sprint.Create(Guid.NewGuid().ToString(), request.TeamId, request.Name, request.StartDate, request.EndDate);
        await _sprintRepository.AddAsync(sprint, cancellationToken);

        return new SprintDto(sprint.Id, sprint.TeamId, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.Status.ToString(), sprint.CreatedOn);
    }
}
