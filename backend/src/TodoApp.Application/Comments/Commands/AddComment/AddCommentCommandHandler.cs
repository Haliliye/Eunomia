using MediatR;
using TodoApp.Application.Comments.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Comments.Commands.AddComment;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IMediator _mediator;

    public AddCommentCommandHandler(
        ICommentRepository commentRepository,
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IActivityRepository activityRepository,
        IMediator mediator)
    {
        _commentRepository = commentRepository;
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _activityRepository = activityRepository;
        _mediator = mediator;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.AuthorId);

        var comment = Comment.Create(
            id: Guid.NewGuid().ToString(),
            userStoryId: request.UserStoryId,
            authorId: request.AuthorId,
            content: request.Content,
            mentionedUserIds: request.MentionedUserIds);

        await _commentRepository.AddAsync(comment, cancellationToken);

        // Comment.Create already raised CommentAddedEvent — dispatching it here
        // triggers CommentAddedEventHandler, which creates the mention
        // notifications (US-114). Keeps that side effect out of this handler.
        await _mediator.PublishDomainEventsAsync(comment, cancellationToken);

        // US-132 AC explicitly lists "commented" as one of the event types the
        // team activity feed should show.
        await _activityRepository.AddAsync(
            Activity.Create(Guid.NewGuid().ToString(), story.TeamId, request.AuthorId, ActivityType.Commented,
                $"commented on \"{story.Title}\"", story.Id),
            cancellationToken);

        return new CommentDto(comment.Id, comment.UserStoryId, comment.AuthorId, comment.Content, comment.MentionedUserIds, comment.CreatedOn, comment.EditedOn);
    }
}
