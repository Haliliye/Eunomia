using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.DeleteStoryTemplate;

public class DeleteStoryTemplateCommandHandler : IRequestHandler<DeleteStoryTemplateCommand>
{
    private readonly ITeamRepository _teamRepository;

    public DeleteStoryTemplateCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(DeleteStoryTemplateCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.DeleteTemplate(request.TemplateId, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
