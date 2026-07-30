using MediatR;

namespace TodoApp.Application.UserStories.Commands.RemoveAttachment;

public record RemoveAttachmentCommand(string UserStoryId, string AttachmentId, string RequestingUserId) : IRequest;
