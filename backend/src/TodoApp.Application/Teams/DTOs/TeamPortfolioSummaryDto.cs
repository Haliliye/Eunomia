namespace TodoApp.Application.Teams.DTOs;

/// <summary>One row of the portfolio overview — a lightweight snapshot per team someone belongs to, without needing a separate dashboard call per team.</summary>
public record TeamPortfolioSummaryDto(
    string TeamId,
    string TeamName,
    int MemberCount,
    int TotalStoryCount,
    int DoneCount,
    int OverdueCount,
    string? ActiveSprintName,
    DateTime? ActiveSprintEndDate);
