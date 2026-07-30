using MediatR;

namespace TodoApp.Application.UserStories.Queries.GetAttachmentDownload;

public record GetAttachmentDownloadQuery(string UserStoryId, string AttachmentId, string RequestingUserId) : IRequest<AttachmentDownloadResult>;

/// <summary>Content is the caller's responsibility to dispose (the controller streams it into the HTTP response).</summary>
public record AttachmentDownloadResult(Stream Content, string ContentType, string FileName);
