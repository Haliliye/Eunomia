using MediatR;
using TodoApp.Application.Comments.DTOs;
using TodoApp.Domain.Comments;

namespace TodoApp.Application.Comments.Queries.GetCommentsByUserStory;

public class GetCommentsByUserStoryQueryHandler : IRequestHandler<GetCommentsByUserStoryQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;

    public GetCommentsByUserStoryQueryHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsByUserStoryQuery request, CancellationToken cancellationToken)
    {
        var comments = await _commentRepository.GetByUserStoryIdAsync(request.UserStoryId, cancellationToken);

        // US-113 AC: comments are ordered chronologically (oldest first).
        return comments
            .OrderBy(c => c.CreatedOn)
            .Select(c => new CommentDto(c.Id, c.UserStoryId, c.AuthorId, c.Content, c.MentionedUserIds, c.CreatedOn, c.EditedOn))
            .ToList();
    }
}
