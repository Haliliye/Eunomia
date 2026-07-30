using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Commands.AddAttachment;

public record AddAttachmentCommand(
    string UserStoryId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    string RequestingUserId) : IRequest<AttachmentDto>;
