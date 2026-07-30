namespace TodoApp.Application.Sprints.Queries.GetSprintBurndown;

public record BurndownPointDto(DateOnly Date, int RemainingCount, int RemainingPoints);

public record SprintBurndownDto(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalPointsAtStart,
    IReadOnlyList<BurndownPointDto> ActualSnapshots);
