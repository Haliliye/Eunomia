using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.UserStories.Queries.GlobalSearch;
using TodoApp.Application.Users.Commands.UpdateNotificationPreferences;
using TodoApp.Application.Users.Queries.GetNotificationPreferences;
using TodoApp.Application.Users.Queries.GetUsersByIds;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Resolves a comma-separated list of user ids to display names — used to
    /// show real names for team members/assignees instead of raw GUIDs in the UI.</summary>
    [HttpGet]
    public async Task<IActionResult> GetByIds([FromQuery] string ids, CancellationToken cancellationToken)
    {
        var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var users = await _mediator.Send(new GetUsersByIdsQuery(idList), cancellationToken);
        return Ok(users);
    }

    [HttpGet("me/notification-preferences")]
    public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken)
    {
        var preferences = await _mediator.Send(new GetNotificationPreferencesQuery(User.GetUserId()), cancellationToken);
        return Ok(preferences);
    }

    [HttpPut("me/notification-preferences")]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateNotificationPreferencesCommand(User.GetUserId(), request.NotifyOnAssignment, request.NotifyOnMention, request.NotifyOnInvitation, request.NotifyOnDueSoon, request.ReminderLeadTimeHours),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Command-palette (Ctrl/Cmd+K) search — across every team the caller is a member of.</summary>
    [HttpGet("me/search")]
    public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new GlobalSearchQuery(User.GetUserId(), keyword), cancellationToken);
        return Ok(results);
    }
}

public record UpdateNotificationPreferencesRequest(bool NotifyOnAssignment, bool NotifyOnMention, bool NotifyOnInvitation, bool NotifyOnDueSoon, int ReminderLeadTimeHours);
