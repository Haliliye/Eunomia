namespace TodoApp.Application.Sprints.DTOs;

public record SprintDto(string Id, string TeamId, string Name, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedOn);
