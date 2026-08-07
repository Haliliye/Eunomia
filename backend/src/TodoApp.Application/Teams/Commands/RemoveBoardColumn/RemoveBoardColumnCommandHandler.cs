using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Teams.Commands.RemoveBoardColumn;

public class RemoveBoardColumnCommandHandler : IRequestHandler<RemoveBoardColumnCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;

    public RemoveBoardColumnCommandHandler(ITeamRepository teamRepository, IUserStoryRepository userStoryRepository)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
    }

    public async Task Handle(RemoveBoardColumnCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // Unlike a label (which can be quietly stripped off every story that
        // had it), a story can't be left with no status at all — so removing
        // a column that's still in use is blocked outright rather than
        // cascaded, and the person is told to move those stories first.
        var storiesUsingColumn = (await _userStoryRepository.GetByTeamIdAsync(request.TeamId, cancellationToken))
            .Count(s => s.Status == request.ColumnKey);
        if (storiesUsingColumn > 0)
            throw new InvalidOperationException($"{storiesUsingColumn} stor{(storiesUsingColumn == 1 ? "y is" : "ies are")} still in this column — move them first.");

        team.RemoveColumn(request.ColumnKey, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
