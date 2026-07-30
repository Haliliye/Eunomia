using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.PersonalTasks.Commands.ConvertPersonalTask;
using TodoApp.Application.PersonalTasks.Commands.CreatePersonalTask;
using TodoApp.Application.PersonalTasks.Commands.DeletePersonalTask;
using TodoApp.Application.PersonalTasks.Commands.TogglePersonalTask;
using TodoApp.Application.PersonalTasks.Commands.UpdatePersonalTask;
using TodoApp.Application.PersonalTasks.Queries.GetMyPersonalTasks;
using TodoApp.Application.PersonalTasks.Queries.GetMyWork;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api")]
public class PersonalTasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public PersonalTasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("personal-tasks")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var tasks = await _mediator.Send(new GetMyPersonalTasksQuery(User.GetUserId()), cancellationToken);
        return Ok(tasks);
    }

    [HttpPost("personal-tasks")]
    public async Task<IActionResult> Create([FromBody] CreatePersonalTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _mediator.Send(new CreatePersonalTaskCommand(User.GetUserId(), request.Title, request.Description, request.DueDate), cancellationToken);
        return Ok(task);
    }

    [HttpPut("personal-tasks/{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePersonalTaskRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdatePersonalTaskCommand(id, User.GetUserId(), request.Title, request.Description, request.DueDate), cancellationToken);
        return NoContent();
    }

    [HttpPut("personal-tasks/{id}/toggle")]
    public async Task<IActionResult> Toggle(string id, [FromBody] TogglePersonalTaskRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TogglePersonalTaskCommand(id, User.GetUserId(), request.IsCompleted), cancellationToken);
        return NoContent();
    }

    [HttpDelete("personal-tasks/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePersonalTaskCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("personal-tasks/{id}/convert")]
    public async Task<IActionResult> Convert(string id, [FromBody] ConvertPersonalTaskRequest request, CancellationToken cancellationToken)
    {
        var story = await _mediator.Send(new ConvertPersonalTaskCommand(id, User.GetUserId(), request.TeamId), cancellationToken);
        return Ok(story);
    }

    /// <summary>US-142: personal tasks + assigned team stories, combined.</summary>
    [HttpGet("my-work")]
    public async Task<IActionResult> GetMyWork(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetMyWorkQuery(User.GetUserId()), cancellationToken);
        return Ok(items);
    }
}

public record CreatePersonalTaskRequest(string Title, string? Description, DateTime? DueDate);
public record UpdatePersonalTaskRequest(string Title, string? Description, DateTime? DueDate);
public record TogglePersonalTaskRequest(bool IsCompleted);
public record ConvertPersonalTaskRequest(string TeamId);
