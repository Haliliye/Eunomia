using MediatR;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Queries.GetPortfolioOverview;

/// <summary>A summary row per team the requester belongs to — for someone managing several teams, a single place to see where each stands without opening each one.</summary>
public record GetPortfolioOverviewQuery(string RequestingUserId) : IRequest<IReadOnlyList<TeamPortfolioSummaryDto>>;
