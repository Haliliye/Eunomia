using System.Text;
using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.Users;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.ExportUserStories;

public class ExportUserStoriesQueryHandler : IRequestHandler<ExportUserStoriesQuery, string>
{
    // A generous cap rather than truly unlimited — protects against a single
    // export request trying to pull an unreasonably huge result set into memory.
    private const int MaxExportRows = 10_000;

    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;

    public ExportUserStoriesQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository, IUserRepository userRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _userRepository = userRepository;
    }

    public async Task<string> Handle(ExportUserStoriesQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var (stories, _) = await _userStoryRepository.SearchAsync(
            request.TeamId, request.Status, request.Priority, request.AssigneeId, request.Keyword,
            page: 1, pageSize: MaxExportRows, request.ShowArchived, request.SprintId, request.LabelId, cancellationToken);

        var labelNamesById = team.Labels.ToDictionary(l => l.Id, l => l.Name);

        // Email, not raw id — a human editing the CSV (and re-importing it,
        // since import expects the same AssigneeEmail column) has no use for
        // an internal id. Team members are the only assignees a story can
        // have, so one batch lookup covers every row.
        var members = await _userRepository.GetByIdsAsync(team.Members.Select(m => m.UserId), cancellationToken);
        var emailById = members.ToDictionary(u => u.Id, u => u.Email);

        var csv = new StringBuilder();
        csv.AppendLine("Title,Description,Status,Priority,AssigneeEmail,DueDate,StoryPoints,Labels");

        foreach (var story in stories)
        {
            var labels = string.Join("; ", story.LabelIds.Select(id => labelNamesById.GetValueOrDefault(id, id)));
            var assigneeEmail = story.AssigneeId is not null ? emailById.GetValueOrDefault(story.AssigneeId, string.Empty) : string.Empty;

            csv.AppendLine(string.Join(",",
                CsvField(story.Title),
                CsvField(story.Description ?? string.Empty),
                CsvField(story.Status.ToString()),
                CsvField(story.Priority.ToString()),
                CsvField(assigneeEmail),
                CsvField(story.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty),
                CsvField(story.StoryPoints?.ToString() ?? string.Empty),
                CsvField(labels)));
        }

        return csv.ToString();
    }

    /// <summary>RFC 4180-style quoting — wraps in quotes and doubles any embedded quotes whenever the value contains a comma, quote, or newline.</summary>
    private static string CsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
