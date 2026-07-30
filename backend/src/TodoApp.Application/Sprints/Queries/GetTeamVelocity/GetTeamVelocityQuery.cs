using MediatR;

namespace TodoApp.Application.Sprints.Queries.GetTeamVelocity;

/// <summary>Completed points across every finished sprint — the trend a real Scrum team wants to see (are we speeding up, slowing down, staying flat).</summary>
public record GetTeamVelocityQuery(string TeamId, string RequestingUserId) : IRequest<IReadOnlyList<VelocityPointDto>>;
