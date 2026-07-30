using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetAttachmentDownload;

public class GetAttachmentDownloadQueryHandler : IRequestHandler<GetAttachmentDownloadQuery, AttachmentDownloadResult>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IAttachmentStorage _attachmentStorage;

    public GetAttachmentDownloadQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository, IAttachmentStorage attachmentStorage)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _attachmentStorage = attachmentStorage;
    }

    public async Task<AttachmentDownloadResult> Handle(GetAttachmentDownloadQuery request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var attachment = story.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId)
            ?? throw new KeyNotFoundException("Attachment not found.");

        var content = await _attachmentStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return new AttachmentDownloadResult(content, attachment.ContentType, attachment.FileName);
    }
}
