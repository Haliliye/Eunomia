using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira;

/// <summary>
/// Turns Jira issues into the same in-memory grid + CsvColumnMapping shape
/// that ImportRowParser already validates CSV rows against — so a Jira
/// import gets identical priority parsing and label matching as a CSV
/// import, without duplicating any of that logic. Status is handled
/// differently from a CSV import: instead of best-effort-guessing one of six
/// fixed columns via a synonym dictionary, the caller (JiraProjectImportService)
/// creates a real board column for every distinct Jira status first and
/// passes the exact status-name -> column-Key mapping in here — so a project
/// with a 9-status workflow gets 9 real columns, not squeezed into six.
/// </summary>
internal static class JiraIssueMapper
{
    private static readonly Dictionary<string, string> PrioritySynonyms = new()
    {
        ["highest"] = "Critical", ["blocker"] = "Critical", ["critical"] = "Critical", ["1"] = "Critical",
        ["high"] = "High", ["2"] = "High",
        ["medium"] = "Medium", ["normal"] = "Medium", ["3"] = "Medium",
        ["low"] = "Low", ["lowest"] = "Low", ["4"] = "Low", ["5"] = "Low",
    };

    private static readonly string[] Headers = { "Title", "Description", "Status", "Priority", "DueDate", "Labels", "AssigneeEmail", "StoryPoints", "JiraIssueKey" };

    public static List<ImportRowDto> MapAndValidate(IReadOnlyList<JiraIssueDto> issues, IReadOnlyDictionary<string, string> columnKeyByStatusName)
    {
        var rows = new List<string[]> { Headers };
        foreach (var issue in issues)
        {
            rows.Add(new[]
            {
                issue.Summary,
                issue.Description ?? string.Empty,
                // Already resolved to a real board column Key (not the raw
                // Jira status name) by EnsureColumnsForStatusesAsync — no
                // further mapping needed, so StatusValueMap below stays empty.
                columnKeyByStatusName.GetValueOrDefault(issue.StatusName, "ToDo"),
                issue.PriorityName ?? string.Empty,
                issue.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                string.Join(";", issue.Labels),
                issue.AssigneeEmail ?? string.Empty,
                issue.StoryPoints?.ToString() ?? string.Empty,
                issue.Key,
            });
        }

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
            StatusValueMap: null,
            PriorityValueMap: priorityValueMap,
            AssigneeEmailColumn: "AssigneeEmail",
            JiraIssueKeyColumn: "JiraIssueKey");

        return ImportRowParser.ParseAndValidate(rows, mapping);
    }

    /// <summary>Every distinct label name across the given issues — used by ImportFromJiraCommandHandler to auto-create any that don't already exist on the target team before applying rows (a label with no matching team label is otherwise silently dropped, same as CSV import).</summary>
    public static IReadOnlyList<string> DistinctLabelNames(IReadOnlyList<JiraIssueDto> issues) =>
        issues.SelectMany(i => i.Labels).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
