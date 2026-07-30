using MediatR;
using TodoApp.Application.Comments.DTOs;

namespace TodoApp.Application.Comments.Commands.AddComment;

public record AddCommentCommand(
    string UserStoryId,
    string AuthorId,
    string Content,
    IReadOnlyCollection<string> MentionedUserIds) : IRequest<CommentDto>;
