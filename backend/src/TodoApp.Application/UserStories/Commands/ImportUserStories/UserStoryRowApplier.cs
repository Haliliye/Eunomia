using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

/// <summary>
/// Turns already-parsed+validated ImportRowDtos into real UserStory
/// aggregates on a team. Shared by ImportUserStoriesCommandHandler (CSV) and
/// ImportFromJiraCommandHandler (Jira OAuth) — the two sources differ only in
/// how they produce ImportRowDtos, not in what happens once you have them.
/// </summary>
internal static class UserStoryRowApplier
{
    // Mirrors UserStory.ChangeStatus's allowed workflow — a story always starts
    // at ToDo, so reaching an arbitrary imported starting status means walking
    // through every intermediate step in order (Debug isn't offered as an
    // import target; nothing in a fresh import would sensibly start there).
    private static readonly Dictionary<string, UserStoryStatus[]> StatusPath = new()
    {
        ["ToDo"] = Array.Empty<UserStoryStatus>(),
        ["Analyze"] = new[] { UserStoryStatus.Analyze },
        ["Dev"] = new[] { UserStoryStatus.Analyze, UserStoryStatus.Dev },
        ["Test"] = new[] { UserStoryStatus.Analyze, UserStoryStatus.Dev, UserStoryStatus.Test },
        ["Done"] = new[] { UserStoryStatus.Analyze, UserStoryStatus.Dev, UserStoryStatus.Test, UserStoryStatus.Done },
    };

    public static async Task<int> ApplyAsync(
        Team team,
        IReadOnlyList<ImportRowDto> rows,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var labelIdByName = team.Labels.ToDictionary(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);
        var createdCount = 0;

        foreach (var row in rows)
        {
            if (!row.IsValid) continue; // invalid rows are skipped, not fatal to the whole import

            var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, row.Title!, row.Description);

            if (row.DueDate.HasValue) story.SetDueDate(row.DueDate);
            if (row.StoryPoints.HasValue) story.SetStoryPoints(row.StoryPoints);
            if (Enum.TryParse<UserStoryPriority>(row.Priority, out var priority)) story.ChangePriority(priority);

            if (StatusPath.TryGetValue(row.Status, out var path))
                foreach (var step in path) story.ChangeStatus(step);

            // Only an email that resolves to an actual team member gets
            // assigned — anything else (typo, someone outside the team, blank)
            // silently leaves the story unassigned rather than failing the row.
            if (!string.IsNullOrWhiteSpace(row.AssigneeEmail))
            {
                var assignee = await userRepository.GetByEmailAsync(row.AssigneeEmail, cancellationToken);
                if (assignee is not null && team.IsMember(assignee.Id))
                    story.Assign(assignee.Id);
            }

            foreach (var labelName in row.LabelNames)
                if (labelIdByName.TryGetValue(labelName, out var labelId))
                    story.AddLabel(labelId);

            await userStoryRepository.AddAsync(story, cancellationToken);
            createdCount++;
        }

        return createdCount;
    }
}
