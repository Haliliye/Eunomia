using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.AddAttachment;

public class AddAttachmentCommandHandler : IRequestHandler<AddAttachmentCommand, AttachmentDto>
{
    // US-134 AC: "a supported type" — a deliberately generous but not
    // wide-open allowlist. Executables/scripts are excluded on purpose.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip"
    };

    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AddAttachmentCommandHandler(
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

    public async Task<AttachmentDto> Handle(AddAttachmentCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // US-134 AC: clear validation error, upload rejected — checked here
        // (before touching disk) rather than relying only on
        // UserStory.AddAttachment's size check, so an unsupported file type
        // is also rejected with the same clear-error treatment.
        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"'{extension}' files aren't supported.");

        if (request.SizeBytes > UserStory.MaxAttachmentSizeBytes)
            throw new ArgumentException($"File exceeds the {UserStory.MaxAttachmentSizeBytes / (1024 * 1024)} MB limit.");

        var storageKey = await _attachmentStorage.SaveAsync(request.Content, cancellationToken);

        var attachment = story.AddAttachment(
            Guid.NewGuid().ToString(), request.FileName, request.ContentType, request.SizeBytes, storageKey, request.RequestingUserId);

        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);

        return new AttachmentDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.UploadedByUserId, attachment.UploadedOn);
    }
}
