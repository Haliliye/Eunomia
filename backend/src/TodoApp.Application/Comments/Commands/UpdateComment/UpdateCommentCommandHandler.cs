using MediatR;
using TodoApp.Application.Comments.DTOs;
using TodoApp.Domain.Comments;

namespace TodoApp.Application.Comments.Commands.UpdateComment;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;

    public UpdateCommentCommandHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new KeyNotFoundException("Comment not found.");

        if (comment.AuthorId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the author can edit this comment.");

        comment.UpdateContent(request.Content, request.MentionedUserIds);
        await _commentRepository.UpdateAsync(comment, cancellationToken);

        return new CommentDto(comment.Id, comment.UserStoryId, comment.AuthorId, comment.Content, comment.MentionedUserIds, comment.CreatedOn, comment.EditedOn);
    }
}
