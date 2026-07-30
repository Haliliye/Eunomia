using MediatR;

namespace TodoApp.Application.UserStories.Queries.GetTeamTimeReport;

/// <summary>US-139: StartDate/EndDate scope which logged-time entries count toward the totals — null means "all time".</summary>
public record GetTeamTimeReportQuery(string TeamId, string RequestingUserId, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<TeamTimeReportDto>;
