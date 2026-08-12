using MediatR;
using TodoApp.Application.Comments.DTOs;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Comments.Queries.GetCommentsByUserStory;

public class GetCommentsByUserStoryQueryHandler : IRequestHandler<GetCommentsByUserStoryQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetCommentsByUserStoryQueryHandler(ICommentRepository commentRepository, IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _commentRepository = commentRepository;
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsByUserStoryQuery request, CancellationToken cancellationToken)
    {
        // Previously missing entirely — any authenticated user could read any
        // story's comments just by knowing/guessing its id.
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");
        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var comments = await _commentRepository.GetByUserStoryIdAsync(request.UserStoryId, cancellationToken);

        // US-113 AC: comments are ordered chronologically (oldest first).
        return comments
            .OrderBy(c => c.CreatedOn)
            .Select(c => new CommentDto(c.Id, c.UserStoryId, c.AuthorId, c.Content, c.MentionedUserIds, c.CreatedOn, c.EditedOn))
            .ToList();
    }
}
