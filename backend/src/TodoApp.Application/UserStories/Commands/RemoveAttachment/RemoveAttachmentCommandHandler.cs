using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.RemoveAttachment;

public class RemoveAttachmentCommandHandler : IRequestHandler<RemoveAttachmentCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public RemoveAttachmentCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IAttachmentStorage attachmentStorage,
        IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _attachmentStorage = attachmentStorage;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(RemoveAttachmentCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var attachment = story.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId)
            ?? throw new KeyNotFoundException("Attachment not found.");

        story.RemoveAttachment(request.AttachmentId);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        // Delete the file itself only after the metadata removal is safely
        // saved — if this delete fails, an orphaned file on disk is a much
        // smaller problem than a dangling reference to a deleted file.
        await _attachmentStorage.DeleteAsync(attachment.StorageKey, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
