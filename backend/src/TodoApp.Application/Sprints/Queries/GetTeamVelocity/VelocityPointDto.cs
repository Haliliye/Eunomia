namespace TodoApp.Application.Sprints.Queries.GetTeamVelocity;

public record VelocityPointDto(string SprintId, string SprintName, DateTime EndDate, int? PlannedPoints, int CompletedPoints);
