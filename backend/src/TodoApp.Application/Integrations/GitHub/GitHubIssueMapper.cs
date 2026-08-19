using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.GitHub;

/// <summary>
/// Turns GitHub issues into the same in-memory grid + CsvColumnMapping shape
/// ImportRowParser already validates CSV rows against — same idea as
/// JiraIssueMapper. GitHub issues only have two states ("open"/"closed"),
/// nothing like Jira's per-project custom workflow, so there's no need for
/// JiraIssueMapper's per-status-name board-column resolution here — "open"
/// maps to whatever this team's first column is, "closed" to "Done".
/// GitHub also has no built-in priority field, so PriorityColumn is left
/// unmapped and every imported story keeps ImportRowParser's own default.
/// </summary>
internal static class GitHubIssueMapper
{
    private static readonly string[] Headers = { "Title", "Description", "Status", "Labels", "AssigneeEmail", "GitHubIssueKey" };

    public static List<ImportRowDto> MapAndValidate(IReadOnlyList<GitHubIssueDto> issues, string owner, string repo, IReadOnlyDictionary<string, string?> emailByLogin)
    {
        var rows = new List<string[]> { Headers };
        foreach (var issue in issues)
        {
            var assigneeEmail = issue.AssigneeLogin is not null && emailByLogin.TryGetValue(issue.AssigneeLogin, out var email) ? email : null;

            rows.Add(new[]
            {
                issue.Title,
                issue.Body ?? string.Empty,
                issue.State == "closed" ? "Done" : "ToDo",
                string.Join(";", issue.Labels),
                assigneeEmail ?? string.Empty,
                $"{owner}/{repo}#{issue.Number}",
            });
        }

        var mapping = new CsvColumnMapping(
            TitleColumn: "Title",
            DescriptionColumn: "Description",
            StatusColumn: "Status",
            PriorityColumn: null,
            DueDateColumn: null,
            StoryPointsColumn: null,
            LabelsColumn: "Labels",
            StatusValueMap: null,
            PriorityValueMap: null,
            AssigneeEmailColumn: "AssigneeEmail",
            GitHubIssueKeyColumn: "GitHubIssueKey");

        return ImportRowParser.ParseAndValidate(rows, mapping);
    }

    /// <summary>Every distinct label name across the given issues — used by ImportFromGitHubCommandHandler to auto-create any that don't already exist on the target team before applying rows.</summary>
    public static IReadOnlyList<string> DistinctLabelNames(IReadOnlyList<GitHubIssueDto> issues) =>
        issues.SelectMany(i => i.Labels).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
