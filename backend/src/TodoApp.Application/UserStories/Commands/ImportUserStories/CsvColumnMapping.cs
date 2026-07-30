namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

/// <summary>
/// Lets the person map THEIR CSV's column headers (whatever Jira, Azure
/// DevOps, or any other tool happened to export) onto our fields, instead of
/// requiring a fixed template. Column names are matched against the CSV's
/// header row case-insensitively. ValueMaps translate the source tool's own
/// vocabulary for status/priority (e.g. Jira's "In Progress", Azure DevOps'
/// "Doing") onto ours — anything not present in the map falls back to the
/// same default ImportRowParser already used (ToDo / Medium).
/// </summary>
public record CsvColumnMapping(
    string TitleColumn,
    string? DescriptionColumn,
    string? StatusColumn,
    string? PriorityColumn,
    string? DueDateColumn,
    string? StoryPointsColumn,
    string? LabelsColumn,
    Dictionary<string, string>? StatusValueMap,
    Dictionary<string, string>? PriorityValueMap);
