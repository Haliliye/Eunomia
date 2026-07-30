using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

public class ImportUserStoriesCommandHandler : IRequestHandler<ImportUserStoriesCommand, ImportSummaryDto>
{
    // Mirrors UserStory.ChangeStatus's allowed workflow — a story always starts
    // at ToDo, so reaching an arbitrary imported starting status means walking
    // through every intermediate step in order (Debug isn't offered as an
    // import target; nothing in a fresh CSV would sensibly start there).
    private static readonly Dictionary<string, UserStoryStatus[]> StatusPath = new()
    {
        ["ToDo"] = Array.Empty<UserStoryStatus>(),
        ["Analyze"] = new[] { UserStoryStatus.Analyze },
        ["Dev"] = new[] { UserStoryStatus.Analyze, UserStoryStatus.Dev },
        ["Test"] = new[] { UserStoryStatus.Analyze, UserStoryStatus.Dev, UserStoryStatus.Test },
        ["Done"] = new[] { UserStoryStatus.Analyze, UserStoryStatus.Dev, UserStoryStatus.Test, UserStoryStatus.Done },
    };

    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ImportUserStoriesCommandHandler(
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ImportSummaryDto> Handle(ImportUserStoriesCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var rows = ImportRowParser.ParseAndValidate(request.CsvContent, request.Mapping);
        var labelIdByName = team.Labels.ToDictionary(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);

        var createdCount = 0;

        foreach (var row in rows)
        {
            if (!row.IsValid) continue; // US-147 AC: invalid rows are skipped, not fatal to the whole import

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
                var assignee = await _userRepository.GetByEmailAsync(row.AssigneeEmail, cancellationToken);
                if (assignee is not null && team.IsMember(assignee.Id))
                    story.Assign(assignee.Id);
            }

            foreach (var labelName in row.LabelNames)
                if (labelIdByName.TryGetValue(labelName, out var labelId))
                    story.AddLabel(labelId);

            await _userStoryRepository.AddAsync(story, cancellationToken);
            createdCount++;
        }

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(createdCount, skippedCount, rows);
    }
}
