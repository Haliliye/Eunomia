using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.AzureDevOps;

/// <summary>
/// Mirrors TodoApp.Application.Integrations.Jira.JiraIssueMapper — turns
/// Azure DevOps work items into the same in-memory grid + CsvColumnMapping
/// shape ImportRowParser already validates against, so an Azure DevOps
/// import gets identical priority parsing and label matching as a CSV or
/// Jira import. Status, like Jira's, is pre-resolved to a real board column
/// key by the caller (see AzureDevOpsProjectImportService.EnsureColumnsForStates)
/// rather than guessed via a synonym dictionary.
/// </summary>
internal static class AzureDevOpsIssueMapper
{
    private static readonly string[] Headers = { "Title", "Description", "Status", "Priority", "DueDate", "Labels", "AssigneeEmail", "StoryPoints", "AzureDevOpsWorkItemId" };

    public static List<ImportRowDto> MapAndValidate(IReadOnlyList<AzureDevOpsWorkItemDto> workItems, IReadOnlyDictionary<string, string> columnKeyByStateName)
    {
        var rows = new List<string[]> { Headers };
        foreach (var item in workItems)
        {
            rows.Add(new[]
            {
                item.Title,
                item.Description ?? string.Empty,
                columnKeyByStateName.GetValueOrDefault(item.StateName, "ToDo"),
                item.PriorityName ?? string.Empty,
                item.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                string.Join(";", item.Tags),
                item.AssigneeEmail ?? string.Empty,
                item.StoryPoints?.ToString() ?? string.Empty,
                item.Id,
            });
        }

        var mapping = new CsvColumnMapping(
            TitleColumn: "Title",
            DescriptionColumn: "Description",
            StatusColumn: "Status",
            PriorityColumn: "Priority",
            DueDateColumn: "DueDate",
            StoryPointsColumn: "StoryPoints",
            LabelsColumn: "Labels",
            StatusValueMap: null,
            // Azure DevOps' numeric priority (1-4) parses straight into our
            // UserStoryPriority enum without a synonym table — the enum's
            // own values are Critical=1/High=2/Medium=3/Low=4, and .NET's
            // Enum.TryParse resolves a numeric string to the matching
            // underlying value automatically.
            PriorityValueMap: null,
            AssigneeEmailColumn: "AssigneeEmail",
            JiraIssueKeyColumn: null,
            AzureDevOpsWorkItemIdColumn: "AzureDevOpsWorkItemId");

        return ImportRowParser.ParseAndValidate(rows, mapping);
    }

    public static IReadOnlyList<string> DistinctLabelNames(IReadOnlyList<AzureDevOpsWorkItemDto> workItems) =>
        workItems.SelectMany(i => i.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
