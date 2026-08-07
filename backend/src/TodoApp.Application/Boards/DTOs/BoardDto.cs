namespace TodoApp.Application.Boards.DTOs;

public record BoardDto(string Id, string TeamId, string Name, string? SprintId, DateTime CreatedOn);
