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
    Dictionary<string, string>? PriorityValueMap,
    // A source's assignee "name" (as in any CSV export) can't be reliably
    // matched to one of our accounts — but a real email address (as Jira's
    // OAuth API provides) can, via IUserRepository.GetByEmailAsync. Optional
    // and defaulted to null so existing CSV-mapping callers are unaffected.
    string? AssigneeEmailColumn = null,
    // Set only by JiraIssueMapper — lets re-importing the same Jira project
    // update existing stories (matched by this key) instead of duplicating
    // them. Never populated for a CSV import: there's no reliable external
    // key in an arbitrary export, so every CSV row always creates a new story.
    string? JiraIssueKeyColumn = null,
    string? AzureDevOpsWorkItemIdColumn = null,
    string? GitHubIssueKeyColumn = null);
