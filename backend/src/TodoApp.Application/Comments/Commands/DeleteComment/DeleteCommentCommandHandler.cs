using MediatR;
using TodoApp.Domain.Comments;

namespace TodoApp.Application.Comments.Commands.DeleteComment;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly ICommentRepository _commentRepository;

    public DeleteCommentCommandHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new KeyNotFoundException("Comment not found.");

        if (comment.AuthorId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the author can delete this comment.");

        await _commentRepository.DeleteAsync(comment.Id, cancellationToken);
    }
}
