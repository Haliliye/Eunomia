using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.CreateLabel;

public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, LabelDto>
{
    private readonly ITeamRepository _teamRepository;

    public CreateLabelCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<LabelDto> Handle(CreateLabelCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        var label = team.CreateLabel(Guid.NewGuid().ToString(), request.Name, request.Color, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);

        return new LabelDto(label.Id, label.Name, label.Color);
    }
}
