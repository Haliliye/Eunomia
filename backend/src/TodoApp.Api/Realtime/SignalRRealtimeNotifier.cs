using Microsoft.AspNetCore.SignalR;
using TodoApp.Application.Common;

namespace TodoApp.Api.Realtime;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<AppHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<AppHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyUserAsync(string userId, object payload, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(GroupNames.User(userId)).SendAsync("notification", payload, cancellationToken);

    public Task NotifyTeamAsync(string teamId, object payload, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(GroupNames.Team(teamId)).SendAsync("teamUpdate", payload, cancellationToken);
}
