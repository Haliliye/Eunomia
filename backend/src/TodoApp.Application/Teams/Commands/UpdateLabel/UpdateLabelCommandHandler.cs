using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.UpdateLabel;

public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand>
{
    private readonly ITeamRepository _teamRepository;

    public UpdateLabelCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(UpdateLabelCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.UpdateLabel(request.LabelId, request.Name, request.Color, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
