using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.DeleteUserStory;

public class DeleteUserStoryCommandHandler : IRequestHandler<DeleteUserStoryCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public DeleteUserStoryCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        ICommentRepository commentRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _commentRepository = commentRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(DeleteUserStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        // Deleting is permanent (unlike Archive, which is reversible) — restrict
        // it to owners/admins rather than any member.
        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        // Cascade: a story's comments have no reason to outlive the story
        // itself, and neither do its subtasks — they're not independently
        // meaningful without the parent (see UserStory.ParentId).
        var subtasks = await _userStoryRepository.GetByParentIdAsync(story.Id, cancellationToken);
        var idsToDelete = new[] { story.Id }.Concat(subtasks.Select(s => s.Id)).ToList();

        await _commentRepository.DeleteByUserStoryIdsAsync(idsToDelete, cancellationToken);
        foreach (var id in idsToDelete)
            await _userStoryRepository.DeleteAsync(id, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
