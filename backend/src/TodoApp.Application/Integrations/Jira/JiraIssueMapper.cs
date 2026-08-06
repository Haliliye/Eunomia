using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira;

/// <summary>
/// Turns Jira issues into the same in-memory grid + CsvColumnMapping shape
/// that ImportRowParser already validates CSV rows against — so a Jira
/// import gets identical status-walking, priority parsing, and label
/// matching as a CSV import, without duplicating any of that logic. The
/// synonym dictionaries below mirror STATUS_SYNONYMS/PRIORITY_SYNONYMS in
/// the frontend's ImportCsvModal (same best-effort Jira vocabulary guesses),
/// kept in sync deliberately so a Jira CSV export and a live Jira OAuth
/// import land on the same status/priority for the same source value.
/// </summary>
internal static class JiraIssueMapper
{
    private static readonly Dictionary<string, string> StatusSynonyms = new()
    {
        ["to do"] = "ToDo", ["backlog"] = "ToDo", ["new"] = "ToDo", ["open"] = "ToDo",
        ["in progress"] = "Dev", ["doing"] = "Dev", ["active"] = "Dev", ["development"] = "Dev",
        ["in review"] = "Test", ["code review"] = "Test", ["testing"] = "Test", ["qa"] = "Test",
        ["blocked"] = "Debug", ["reopened"] = "Debug",
        ["done"] = "Done", ["closed"] = "Done", ["resolved"] = "Done", ["completed"] = "Done",
        ["analyze"] = "Analyze", ["analysis"] = "Analyze", ["design"] = "Analyze",
    };

    private static readonly Dictionary<string, string> PrioritySynonyms = new()
    {
        ["highest"] = "Critical", ["blocker"] = "Critical", ["critical"] = "Critical", ["1"] = "Critical",
        ["high"] = "High", ["2"] = "High",
        ["medium"] = "Medium", ["normal"] = "Medium", ["3"] = "Medium",
        ["low"] = "Low", ["lowest"] = "Low", ["4"] = "Low", ["5"] = "Low",
    };

    private static readonly string[] Headers = { "Title", "Description", "Status", "Priority", "DueDate", "Labels", "AssigneeEmail", "StoryPoints" };

    public static List<ImportRowDto> MapAndValidate(IReadOnlyList<JiraIssueDto> issues)
    {
        var rows = new List<string[]> { Headers };
        foreach (var issue in issues)
        {
            rows.Add(new[]
            {
                issue.Summary,
                issue.Description ?? string.Empty,
                issue.StatusName,
                issue.PriorityName ?? string.Empty,
                issue.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                string.Join(";", issue.Labels),
                issue.AssigneeEmail ?? string.Empty,
                issue.StoryPoints?.ToString() ?? string.Empty,
            });
        }

        var statusValueMap = issues
            .Select(i => i.StatusName)
            .Distinct()
            .ToDictionary(s => s, s => StatusSynonyms.GetValueOrDefault(s.ToLowerInvariant(), string.Empty));
        var priorityValueMap = issues
            .Select(i => i.PriorityName)
            .Where(p => p is not null)
            .Distinct()
            .ToDictionary(p => p!, p => PrioritySynonyms.GetValueOrDefault(p!.ToLowerInvariant(), string.Empty));

        var mapping = new CsvColumnMapping(
            TitleColumn: "Title",
            DescriptionColumn: "Description",
            StatusColumn: "Status",
            PriorityColumn: "Priority",
            DueDateColumn: "DueDate",
            StoryPointsColumn: "StoryPoints",
            LabelsColumn: "Labels",
            StatusValueMap: statusValueMap,
            PriorityValueMap: priorityValueMap,
            AssigneeEmailColumn: "AssigneeEmail");

        return ImportRowParser.ParseAndValidate(rows, mapping);
    }

    /// <summary>Every distinct label name across the given issues — used by ImportFromJiraCommandHandler to auto-create any that don't already exist on the target team before applying rows (a label with no matching team label is otherwise silently dropped, same as CSV import).</summary>
    public static IReadOnlyList<string> DistinctLabelNames(IReadOnlyList<JiraIssueDto> issues) =>
        issues.SelectMany(i => i.Labels).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
