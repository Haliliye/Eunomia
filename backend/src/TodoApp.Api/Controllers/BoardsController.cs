using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.Boards.Commands.CreateBoard;
using TodoApp.Application.Boards.Commands.DeleteBoard;
using TodoApp.Application.Boards.Commands.RenameBoard;
using TodoApp.Application.Boards.Queries.GetBoardsByTeam;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api")]
public class BoardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BoardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("teams/{teamId}/boards")]
    public async Task<IActionResult> GetByTeam(string teamId, CancellationToken cancellationToken)
    {
        var boards = await _mediator.Send(new GetBoardsByTeamQuery(teamId, User.GetUserId()), cancellationToken);
        return Ok(boards);
    }

    [HttpPost("teams/{teamId}/boards")]
    public async Task<IActionResult> Create(string teamId, [FromBody] CreateBoardRequest request, CancellationToken cancellationToken)
    {
        var board = await _mediator.Send(new CreateBoardCommand(teamId, request.Name, request.SprintId, User.GetUserId()), cancellationToken);
        return Ok(board);
    }

    public record CreateBoardRequest(string Name, string? SprintId);

    [HttpPut("boards/{boardId}")]
    public async Task<IActionResult> Rename(string boardId, [FromBody] RenameBoardRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RenameBoardCommand(boardId, request.Name, request.SprintId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    public record RenameBoardRequest(string Name, string? SprintId);

    [HttpDelete("boards/{boardId}")]
    public async Task<IActionResult> Delete(string boardId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBoardCommand(boardId, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}
