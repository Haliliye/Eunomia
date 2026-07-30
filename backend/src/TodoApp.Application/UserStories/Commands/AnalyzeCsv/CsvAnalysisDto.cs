namespace TodoApp.Application.UserStories.Commands.AnalyzeCsv;

/// <summary>DistinctStatusValues/DistinctPriorityValues are populated by the
/// frontend's own follow-up call once it knows which columns map to status/
/// priority — this DTO only carries what's knowable before any mapping exists.</summary>
public record CsvAnalysisDto(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> SampleRows, int TotalDataRows);
