using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TodoApp.Api.Common;
using TodoApp.Domain.Teams;

namespace TodoApp.Api.Realtime;

/// <summary>
/// One hub for both kinds of live updates the app needs:
///   - "user:{userId}" group — personal notifications (joined automatically on connect).
///   - "team:{teamId}" group — board/story updates for whoever has that team open
///     (joined explicitly via JoinTeam, e.g. when BoardPage mounts).
/// </summary>
[Authorize]
public class AppHub : Hub
{
    private readonly ITeamRepository _teamRepository;

    public AppHub(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId();
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.User(userId));

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Previously missing entirely — any authenticated connection could join
    /// any team's live-update group just by knowing/guessing its id, and
    /// from then on silently receive that team's board/story change events.
    /// Only members get to.
    /// </summary>
    public async Task JoinTeam(string teamId)
    {
        var userId = Context.User?.GetUserId();
        if (userId is null) return;

        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team is null || !team.IsMember(userId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Team(teamId));
    }

    public Task LeaveTeam(string teamId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Team(teamId));
}

public static class GroupNames
{
    public static string User(string userId) => $"user:{userId}";
    public static string Team(string teamId) => $"team:{teamId}";
}
