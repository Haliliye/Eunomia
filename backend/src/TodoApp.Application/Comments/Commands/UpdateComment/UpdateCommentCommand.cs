using MediatR;
using TodoApp.Application.Comments.DTOs;

namespace TodoApp.Application.Comments.Commands.UpdateComment;

public record UpdateCommentCommand(string CommentId, string RequestingUserId, string Content, IReadOnlyCollection<string> MentionedUserIds) : IRequest<CommentDto>;
