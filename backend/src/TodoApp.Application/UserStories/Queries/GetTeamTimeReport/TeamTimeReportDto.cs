namespace TodoApp.Application.UserStories.Queries.GetTeamTimeReport;

public record StoryTimeReportRow(string StoryId, string Title, double? EstimatedHours, double LoggedHours, double? Variance);

public record TeamTimeReportDto(IReadOnlyList<StoryTimeReportRow> Rows, double TotalEstimatedHours, double TotalLoggedHours);
