using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TodoApp.Api.Common;

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
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId();
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.User(userId));

        await base.OnConnectedAsync();
    }

    public Task JoinTeam(string teamId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Team(teamId));

    public Task LeaveTeam(string teamId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Team(teamId));
}

public static class GroupNames
{
    public static string User(string userId) => $"user:{userId}";
    public static string Team(string teamId) => $"team:{teamId}";
}
