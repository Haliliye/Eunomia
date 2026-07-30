using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Queries.GetTeamDashboard;

/// <summary>SprintId narrows the dashboard to just that sprint's stories — null means the whole team, same as before.</summary>
public record GetTeamDashboardQuery(string TeamId, string RequestingUserId, string? SprintId = null) : IRequest<TeamDashboardDto>;
