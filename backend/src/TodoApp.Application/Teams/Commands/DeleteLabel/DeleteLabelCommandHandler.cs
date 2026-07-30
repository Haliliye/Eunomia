using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Teams.Commands.DeleteLabel;

public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;

    public DeleteLabelCommandHandler(ITeamRepository teamRepository, IUserStoryRepository userStoryRepository)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
    }

    public async Task Handle(DeleteLabelCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // Team.DeleteLabel only removes the label from the team's own list —
        // it has no access to IUserStoryRepository, so the cascade (US-125 AC:
        // "deleting a label removes it from all user stories it was applied to")
        // happens here instead.
        team.DeleteLabel(request.LabelId, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);

        var stories = await _userStoryRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        foreach (var story in stories.Where(s => s.LabelIds.Contains(request.LabelId)))
        {
            story.RemoveLabel(request.LabelId);
            await _userStoryRepository.UpdateAsync(story, cancellationToken);
        }
    }
}
