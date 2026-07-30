using MediatR;

namespace TodoApp.Application.UserStories.Commands.AnalyzeCsv;

/// <summary>US-147 extended: the first step of importing ANY CSV export (Jira, Azure DevOps, or our own) — just reads the shape, maps nothing yet.</summary>
public record AnalyzeCsvCommand(string TeamId, string RequestingUserId, string CsvContent) : IRequest<CsvAnalysisDto>;
