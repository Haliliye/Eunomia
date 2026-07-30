using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.Sprints.Commands.CompleteSprint;
using TodoApp.Application.Sprints.Commands.CreateSprint;
using TodoApp.Application.Sprints.Commands.StartSprint;
using TodoApp.Application.Sprints.Queries.GetSprintBurndown;
using TodoApp.Application.Sprints.Queries.GetTeamSprints;
using TodoApp.Application.Sprints.Queries.GetTeamVelocity;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api")]
public class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SprintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("teams/{teamId}/sprints")]
    public async Task<IActionResult> GetForTeam(string teamId, CancellationToken cancellationToken)
    {
        var sprints = await _mediator.Send(new GetTeamSprintsQuery(teamId, User.GetUserId()), cancellationToken);
        return Ok(sprints);
    }

    [HttpPost("teams/{teamId}/sprints")]
    public async Task<IActionResult> Create(string teamId, [FromBody] CreateSprintRequest request, CancellationToken cancellationToken)
    {
        var sprint = await _mediator.Send(new CreateSprintCommand(teamId, request.Name, request.StartDate, request.EndDate, User.GetUserId()), cancellationToken);
        return Ok(sprint);
    }

    [HttpPut("sprints/{id}/start")]
    public async Task<IActionResult> Start(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new StartSprintCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("sprints/{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CompleteSprintCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>Classic Kanban/Scrum burndown — remaining count/points per day
    /// since the sprint started, plus enough info for the frontend to draw
    /// the "ideal" line (TotalPointsAtStart down to 0 across StartDate..EndDate).</summary>
    [HttpGet("sprints/{id}/burndown")]
    public async Task<IActionResult> GetBurndown(string id, CancellationToken cancellationToken)
    {
        var burndown = await _mediator.Send(new GetSprintBurndownQuery(id, User.GetUserId()), cancellationToken);
        return Ok(burndown);
    }

    /// <summary>Completed points per finished sprint — the team velocity trend chart.</summary>
    [HttpGet("teams/{teamId}/velocity")]
    public async Task<IActionResult> GetVelocity(string teamId, CancellationToken cancellationToken)
    {
        var velocity = await _mediator.Send(new GetTeamVelocityQuery(teamId, User.GetUserId()), cancellationToken);
        return Ok(velocity);
    }
}

public record CreateSprintRequest(string Name, DateTime StartDate, DateTime EndDate);
