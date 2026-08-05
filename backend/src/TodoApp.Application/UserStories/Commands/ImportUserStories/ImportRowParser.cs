using TodoApp.Application.Common;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

/// <summary>
/// Shared row-parsing/validation for both PreviewImportUserStoriesCommand and
/// ImportUserStoriesCommand — the two must agree exactly on what's valid, so
/// what the person previewed is exactly what gets created.
///
/// Column positions come from the mapping the person chose (see
/// CsvColumnMapping), matched against the CSV's own header row by name — this
/// is what lets a Jira or Azure DevOps export (or anything else) work without
/// forcing their columns into our exact template first.
/// </summary>
internal static class ImportRowParser
{
    public static List<ImportRowDto> ParseAndValidate(string csvContent, CsvColumnMapping mapping) =>
        ParseAndValidate(CsvParser.Parse(csvContent), mapping);

    /// <summary>
    /// Same validation/mapping as the CSV-text overload, but takes already-tabular
    /// rows directly — lets a non-CSV source (e.g. Jira's REST API, see
    /// ImportFromJiraCommandHandler) build an in-memory header+data grid and
    /// reuse this exact logic instead of round-tripping through CSV text.
    /// </summary>
    public static List<ImportRowDto> ParseAndValidate(IReadOnlyList<string[]> rows, CsvColumnMapping mapping)
    {
        var results = new List<ImportRowDto>();
        if (rows.Count == 0) return results;

        var headers = rows[0];
        var indexByHeader = headers
            .Select((header, index) => (header, index))
            .ToDictionary(x => x.header.Trim(), x => x.index, StringComparer.OrdinalIgnoreCase);

        int? IndexFor(string? columnName) =>
            columnName is not null && indexByHeader.TryGetValue(columnName, out var index) ? index : null;

        var titleIndex = IndexFor(mapping.TitleColumn)
            ?? throw new ArgumentException($"Column \"{mapping.TitleColumn}\" (mapped to Title) wasn't found in this CSV's header row.");
        var descriptionIndex = IndexFor(mapping.DescriptionColumn);
        var statusIndex = IndexFor(mapping.StatusColumn);
        var priorityIndex = IndexFor(mapping.PriorityColumn);
        var dueDateIndex = IndexFor(mapping.DueDateColumn);
        var storyPointsIndex = IndexFor(mapping.StoryPointsColumn);
        var labelsIndex = IndexFor(mapping.LabelsColumn);

        // Skip the header row.
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 1; // 1-based, matching what a spreadsheet app would show

            string Col(int? index) => index.HasValue && index.Value < row.Length ? row[index.Value].Trim() : string.Empty;

            var title = Col(titleIndex);
            if (string.IsNullOrWhiteSpace(title))
            {
                results.Add(new ImportRowDto(rowNumber, false, "Title is required.", null, null, "ToDo", "Medium", null, null, null, Array.Empty<string>()));
                continue;
            }

            var description = Col(descriptionIndex);

            var status = ResolveMapped(Col(statusIndex), mapping.StatusValueMap, Domain.UserStories.UserStoryStatus.ToDo.ToString(),
                v => Enum.TryParse<Domain.UserStories.UserStoryStatus>(v, ignoreCase: true, out _));
            var priority = ResolveMapped(Col(priorityIndex), mapping.PriorityValueMap, Domain.UserStories.UserStoryPriority.Medium.ToString(),
                v => Enum.TryParse<Domain.UserStories.UserStoryPriority>(v, ignoreCase: true, out _));

            var dueDate = DateTime.TryParse(Col(dueDateIndex), out var parsedDate) ? parsedDate : (DateTime?)null;
            var storyPoints = int.TryParse(Col(storyPointsIndex), out var parsedPoints) && parsedPoints >= 0 ? parsedPoints : (int?)null;
            var labelNames = Col(labelsIndex)
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // Importing an external tool's assignee (a display name/username,
            // not an email) can't be reliably matched to one of our accounts —
            // left unassigned rather than guessing wrong. See README.
            results.Add(new ImportRowDto(rowNumber, true, null, title, string.IsNullOrWhiteSpace(description) ? null : description,
                status, priority, null, dueDate, storyPoints, labelNames));
        }

        return results;
    }

    /// <summary>
    /// Resolves a raw source value (e.g. Jira's "In Progress") to one of our
    /// enum names via the person's chosen value map. Falls back, in order, to:
    /// the raw value itself if it already happens to be a valid enum name
    /// (case-insensitive) — covers round-tripping our own export — then the
    /// given default.
    /// </summary>
    private static string ResolveMapped(string rawValue, Dictionary<string, string>? valueMap, string defaultValue, Func<string, bool> isValidEnumName)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return defaultValue;

        if (valueMap is not null && valueMap.TryGetValue(rawValue, out var mapped) && isValidEnumName(mapped))
            return mapped;

        return isValidEnumName(rawValue) ? rawValue : defaultValue;
    }
}
