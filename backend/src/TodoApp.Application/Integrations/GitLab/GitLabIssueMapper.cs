using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.GitLab;

/// <summary>
/// Turns GitLab issues into the same in-memory grid + CsvColumnMapping shape
/// ImportRowParser already validates CSV rows against — same idea as
/// GitHubIssueMapper. GitLab issues only have two states ("opened"/"closed"),
/// so "opened" maps to "ToDo" and "closed" to "Done", same simplification
/// GitHubIssueMapper makes. No built-in priority field either, so
/// PriorityColumn is left unmapped.
/// </summary>
internal static class GitLabIssueMapper
{
    private static readonly string[] Headers = { "Title", "Description", "Status", "Labels", "AssigneeEmail", "GitLabIssueKey" };

    public static List<ImportRowDto> MapAndValidate(IReadOnlyList<GitLabIssueDto> issues, string pathWithNamespace, IReadOnlyDictionary<string, string?> emailByUsername)
    {
        var rows = new List<string[]> { Headers };
        foreach (var issue in issues)
        {
            var assigneeEmail = issue.AssigneeUsername is not null && emailByUsername.TryGetValue(issue.AssigneeUsername, out var email) ? email : null;

            rows.Add(new[]
            {
                issue.Title,
                issue.Description ?? string.Empty,
                issue.State == "closed" ? "Done" : "ToDo",
                string.Join(";", issue.Labels),
                assigneeEmail ?? string.Empty,
                $"{pathWithNamespace}#{issue.Iid}",
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
            GitLabIssueKeyColumn: "GitLabIssueKey");

        return ImportRowParser.ParseAndValidate(rows, mapping);
    }

    /// <summary>Every distinct label name across the given issues — used by ImportFromGitLabCommandHandler to auto-create any that don't already exist on the target team before applying rows.</summary>
    public static IReadOnlyList<string> DistinctLabelNames(IReadOnlyList<GitLabIssueDto> issues) =>
        issues.SelectMany(i => i.Labels).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
