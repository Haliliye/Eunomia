using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.Invitations.Queries.GetMyInvitations;
using TodoApp.Application.Teams.Commands.AcceptInvitation;
using TodoApp.Application.Teams.Commands.CancelInvitation;
using TodoApp.Application.Teams.Commands.DeclineInvitation;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The current user's pending "join this team" invitations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var invitations = await _mediator.Send(new GetMyInvitationsQuery(User.GetUserId()), cancellationToken);
        return Ok(invitations);
    }

    [HttpPut("{id}/accept")]
    public async Task<IActionResult> Accept(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AcceptInvitationCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/decline")]
    public async Task<IActionResult> Decline(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeclineInvitationCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>Withdraws a still-pending invitation (inviter or team owner only).</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelInvitationCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}
