using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.Activities.Queries.GetTeamActivity;
using TodoApp.Application.Invitations.Queries.GetTeamInvitations;
using TodoApp.Application.Teams.Commands.CreateLabel;
using TodoApp.Application.Teams.Commands.CreateTeam;
using TodoApp.Application.Teams.Commands.DeleteLabel;
using TodoApp.Application.Teams.Commands.DeleteTeam;
using TodoApp.Application.Teams.Commands.InviteTeamMember;
using TodoApp.Application.Teams.Commands.RemoveTeamMember;
using TodoApp.Application.Teams.Commands.CreateStoryTemplate;
using TodoApp.Application.Teams.Commands.DeleteStoryTemplate;
using TodoApp.Application.Teams.Commands.SetColumnWipLimit;
using TodoApp.Application.Teams.Commands.SetMemberRole;
using TodoApp.Application.UserStories.Queries.GetTeamTimeReport;
using TodoApp.Application.Teams.Commands.UpdateLabel;
using TodoApp.Application.Teams.Commands.UpdateTeam;
using TodoApp.Application.Teams.Queries.GetTeamById;
using TodoApp.Application.Teams.Queries.GetTeams;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Every "who's doing this" value below comes from the JWT (User.GetUserId()),
    // never from the request body/query — the caller can't act as someone else.

    [HttpGet]
    public async Task<IActionResult> GetMyTeams([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var teams = await _mediator.Send(new GetTeamsQuery(User.GetUserId(), page, pageSize), cancellationToken);
        return Ok(teams);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var team = await _mediator.Send(new GetTeamByIdQuery(id, User.GetUserId()), cancellationToken);
        return team is null ? NotFound() : Ok(team);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await _mediator.Send(
            new CreateTeamCommand(request.Name, request.Description, User.GetUserId()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateTeamCommand(id, request.Name, request.Description, User.GetUserId()),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTeamCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>Sends a pending invitation — the invited person is only added
    /// to the team once they accept it (see InvitationsController).</summary>
    [HttpPost("{id}/invitations")]
    public async Task<IActionResult> InviteMember(string id, [FromBody] InviteTeamMemberRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new InviteTeamMemberCommand(id, request.Email, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>Outstanding (pending) invitations for this team — so the owner can see and cancel them.</summary>
    [HttpGet("{id}/invitations")]
    public async Task<IActionResult> GetInvitations(string id, CancellationToken cancellationToken)
    {
        var invitations = await _mediator.Send(new GetTeamInvitationsQuery(id), cancellationToken);
        return Ok(invitations);
    }

    /// <summary>Recent "who did what" entries for the Summary tab.</summary>
    [HttpGet("{id}/activity")]
    public async Task<IActionResult> GetActivity(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? actorUserId = null,
        [FromQuery] string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        var activity = await _mediator.Send(new GetTeamActivityQuery(id, User.GetUserId(), page, pageSize, actorUserId, actionType), cancellationToken);
        return Ok(activity);
    }

    /// <summary>US-139: extends the dashboard with an estimate-vs-actual view.</summary>
    [HttpGet("{id}/time-report")]
    public async Task<IActionResult> GetTimeReport(string id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken cancellationToken)
    {
        var report = await _mediator.Send(new GetTeamTimeReportQuery(id, User.GetUserId(), startDate, endDate), cancellationToken);
        return Ok(report);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveTeamMemberCommand(id, userId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>Owner-only: promotes a member to Admin or demotes an Admin back to Member.</summary>
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> SetMemberRole(string id, string userId, [FromBody] SetMemberRoleRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetMemberRoleCommand(id, userId, request.Role, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/labels")]
    public async Task<IActionResult> CreateLabel(string id, [FromBody] CreateLabelRequest request, CancellationToken cancellationToken)
    {
        var label = await _mediator.Send(new CreateLabelCommand(id, request.Name, request.Color, User.GetUserId()), cancellationToken);
        return Ok(label);
    }

    [HttpPut("{id}/labels/{labelId}")]
    public async Task<IActionResult> UpdateLabel(string id, string labelId, [FromBody] UpdateLabelRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateLabelCommand(id, labelId, request.Name, request.Color, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/labels/{labelId}")]
    public async Task<IActionResult> DeleteLabel(string id, string labelId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLabelCommand(id, labelId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>Owner-only, optional Kanban feature — null limit removes the cap for that column.</summary>
    [HttpPut("{id}/wip-limits/{status}")]
    public async Task<IActionResult> SetWipLimit(string id, string status, [FromBody] SetWipLimitRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetColumnWipLimitCommand(id, status, request.Limit, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/templates")]
    public async Task<IActionResult> CreateTemplate(string id, [FromBody] CreateStoryTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await _mediator.Send(
            new CreateStoryTemplateCommand(id, request.Name, request.DefaultDescription, request.DefaultPriority, request.ChecklistItemTexts, User.GetUserId()),
            cancellationToken);
        return Ok(template);
    }

    [HttpDelete("{id}/templates/{templateId}")]
    public async Task<IActionResult> DeleteTemplate(string id, string templateId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteStoryTemplateCommand(id, templateId, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}

public record SetMemberRoleRequest(string Role);
public record CreateLabelRequest(string Name, string Color);
public record UpdateLabelRequest(string Name, string Color);
public record SetWipLimitRequest(int? Limit);
public record CreateStoryTemplateRequest(string Name, string? DefaultDescription, string? DefaultPriority, List<string> ChecklistItemTexts);

public record CreateTeamRequest(string Name, string? Description);
public record UpdateTeamRequest(string Name, string? Description);
public record InviteTeamMemberRequest(string Email);
