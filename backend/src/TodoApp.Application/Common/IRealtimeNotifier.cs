namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction so Application/handlers don't depend on SignalR directly —
/// the actual hub lives in the Api project (Realtime/AppHub.cs) since hubs
/// are a transport concern, not an application one. "payload" is forwarded
/// as-is (usually a DTO); this interface doesn't care what's inside it.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>Pushes to one specific user (e.g. a new notification for them).</summary>
    Task NotifyUserAsync(string userId, object payload, CancellationToken cancellationToken = default);

    /// <summary>Pushes to everyone currently viewing a team (e.g. the board should refresh).</summary>
    Task NotifyTeamAsync(string teamId, object payload, CancellationToken cancellationToken = default);
}
