using MediatR;

namespace TodoApp.Application.Comments.Commands.DeleteComment;

public record DeleteCommentCommand(string CommentId, string RequestingUserId) : IRequest;
