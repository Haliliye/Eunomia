using MediatR;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Teams.Commands.DeleteTeam;

public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ICommentRepository _commentRepository;

    public DeleteTeamCommandHandler(
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        ICommentRepository commentRepository)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _commentRepository = commentRepository;
    }

    public async Task Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        var isOwner = team.Members.Any(m => m.UserId == request.RequestingUserId && m.Role == TeamRole.Owner);
        if (!isOwner)
            throw new UnauthorizedAccessException("Only the team owner can delete the team.");

        // US-103 AC: "the team AND ALL ITS USER STORIES are removed" — cascade
        // through stories and their comments before removing the team itself.
        // NOTE: hard delete for this skeleton; swap for a soft-delete/archive
        // flag on the repositories if you want teams/user stories recoverable.
        var stories = await _userStoryRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        var storyIds = stories.Select(s => s.Id).ToList();

        await _commentRepository.DeleteByUserStoryIdsAsync(storyIds, cancellationToken);
        await _userStoryRepository.DeleteByTeamIdAsync(request.TeamId, cancellationToken);
        await _teamRepository.DeleteAsync(request.TeamId, cancellationToken);
    }
}
