using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.Comments.Commands.AddComment;
using TodoApp.Application.Comments.Commands.DeleteComment;
using TodoApp.Application.Comments.Commands.UpdateComment;
using TodoApp.Application.Comments.Queries.GetCommentsByUserStory;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserStory([FromQuery] string userStoryId, CancellationToken cancellationToken)
    {
        var comments = await _mediator.Send(new GetCommentsByUserStoryQuery(userStoryId), cancellationToken);
        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await _mediator.Send(
            new AddCommentCommand(request.UserStoryId, User.GetUserId(), request.Content, request.MentionedUserIds ?? Array.Empty<string>()),
            cancellationToken);

        return Ok(comment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await _mediator.Send(
            new UpdateCommentCommand(id, User.GetUserId(), request.Content, request.MentionedUserIds ?? Array.Empty<string>()),
            cancellationToken);

        return Ok(comment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCommentCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}

public record AddCommentRequest(string UserStoryId, string Content, string[]? MentionedUserIds);
public record UpdateCommentRequest(string Content, string[]? MentionedUserIds);
