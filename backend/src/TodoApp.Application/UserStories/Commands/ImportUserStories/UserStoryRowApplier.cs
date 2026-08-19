using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

/// <summary>Story ids keyed by each source's own issue identifier — lets each import service resolve issue links/comments/attachments against the right story right after this batch, without a second DB round trip per row.</summary>
internal record ApplyResult(
    int CreatedCount,
    int UpdatedCount,
    IReadOnlyDictionary<string, string> StoryIdByJiraKey,
    IReadOnlyDictionary<string, string> StoryIdByAzureDevOpsWorkItemId,
    IReadOnlyDictionary<string, string> StoryIdByGitHubIssueKey);

/// <summary>
/// Turns already-parsed+validated ImportRowDtos into real UserStory
/// aggregates on a team. Shared by ImportUserStoriesCommandHandler (CSV),
/// JiraProjectImportService, AzureDevOpsProjectImportService, and
/// GitHubProjectImportService — the sources differ only in how they produce
/// ImportRowDtos, not in what happens once you have them.
///
/// Rows carrying a JiraIssueKey, AzureDevOpsWorkItemId, or GitHubIssueKey are
/// matched against existing stories with that same key/id on this team and
/// updated in place instead of creating a duplicate — this is what makes
/// re-importing the same project safe to run repeatedly.
/// </summary>
internal static class UserStoryRowApplier
{
    public static async Task<ApplyResult> ApplyAsync(
        Team team,
        IReadOnlyList<ImportRowDto> rows,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        string requestingUserId,
        CancellationToken cancellationToken)
    {
        var labelIdByName = team.Labels.ToDictionary(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);

        var jiraKeys = rows.Where(r => r.IsValid && r.JiraIssueKey is not null).Select(r => r.JiraIssueKey!).Distinct().ToList();
        var existingByJiraKey = (await userStoryRepository.GetByJiraIssueKeysAsync(team.Id, jiraKeys, cancellationToken))
            .Where(s => s.JiraIssueKey is not null)
            .ToDictionary(s => s.JiraIssueKey!, s => s);

        var workItemIds = rows.Where(r => r.IsValid && r.AzureDevOpsWorkItemId is not null).Select(r => r.AzureDevOpsWorkItemId!).Distinct().ToList();
        var existingByWorkItemId = (await userStoryRepository.GetByAzureDevOpsWorkItemIdsAsync(team.Id, workItemIds, cancellationToken))
            .Where(s => s.AzureDevOpsWorkItemId is not null)
            .ToDictionary(s => s.AzureDevOpsWorkItemId!, s => s);

        var gitHubKeys = rows.Where(r => r.IsValid && r.GitHubIssueKey is not null).Select(r => r.GitHubIssueKey!).Distinct().ToList();
        var existingByGitHubKey = (await userStoryRepository.GetByGitHubIssueKeysAsync(team.Id, gitHubKeys, cancellationToken))
            .Where(s => s.GitHubIssueKey is not null)
            .ToDictionary(s => s.GitHubIssueKey!, s => s);

        var createdCount = 0;
        var updatedCount = 0;
        var storyIdByJiraKey = new Dictionary<string, string>();
        var storyIdByWorkItemId = new Dictionary<string, string>();
        var storyIdByGitHubKey = new Dictionary<string, string>();

        foreach (var row in rows)
        {
            if (!row.IsValid) continue; // invalid rows are skipped, not fatal to the whole import

            var existing = (row.JiraIssueKey is not null && existingByJiraKey.TryGetValue(row.JiraIssueKey, out var byJira)) ? byJira
                : (row.AzureDevOpsWorkItemId is not null && existingByWorkItemId.TryGetValue(row.AzureDevOpsWorkItemId, out var byWorkItem)) ? byWorkItem
                : (row.GitHubIssueKey is not null && existingByGitHubKey.TryGetValue(row.GitHubIssueKey, out var byGitHub)) ? byGitHub
                : null;

            UserStory story;
            if (existing is not null)
            {
                story = existing;
                story.UpdateDetails(row.Title!, row.Description);
            }
            else
            {
                // The importer is recorded as reporter — the source doesn't
                // reliably map to one of our accounts (same reasoning as
                // AssigneeEmail's fallback), and "who ran the import" is a
                // more useful fact than "unknown" anyway.
                story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, row.Title!, row.Description,
                    createdByUserId: requestingUserId, jiraIssueKey: row.JiraIssueKey, azureDevOpsWorkItemId: row.AzureDevOpsWorkItemId,
                    gitHubIssueKey: row.GitHubIssueKey);
            }

            if (row.DueDate.HasValue) story.SetDueDate(row.DueDate);
            if (row.StoryPoints.HasValue) story.SetStoryPoints(row.StoryPoints);
            if (Enum.TryParse<UserStoryPriority>(row.Priority, out var priority)) story.ChangePriority(priority);
            // Status is a per-team board column key (see BoardColumn), not a
            // fixed enum — only apply it if it actually matches one of this
            // team's real columns; an unrecognized source value (e.g. a CSV
            // export using a workflow this team doesn't have) leaves the
            // story at its default ("ToDo") rather than creating a
            // never-shown-on-any-column "ghost" status.
            if (team.Columns.Any(c => c.Key == row.Status)) story.ChangeStatus(row.Status);

            // Only an email that resolves to an actual team member gets
            // assigned — anything else (typo, someone outside the team, blank)
            // silently leaves the story unassigned rather than failing the row.
            if (!string.IsNullOrWhiteSpace(row.AssigneeEmail))
            {
                var assignee = await userRepository.GetByEmailAsync(row.AssigneeEmail, cancellationToken);
                if (assignee is not null && team.IsMember(assignee.Id))
                    story.Assign(assignee.Id);
            }

            // Additive only, even on update — a label present on the team but
            // no longer on the source item isn't removed, since someone may
            // have applied it for Eunomia-only reasons after the last import.
            foreach (var labelName in row.LabelNames)
                if (labelIdByName.TryGetValue(labelName, out var labelId))
                    story.AddLabel(labelId);

            if (existing is not null)
            {
                await userStoryRepository.UpdateAsync(story, cancellationToken);
                updatedCount++;
            }
            else
            {
                await userStoryRepository.AddAsync(story, cancellationToken);
                createdCount++;
            }

            if (row.JiraIssueKey is not null) storyIdByJiraKey[row.JiraIssueKey] = story.Id;
            if (row.AzureDevOpsWorkItemId is not null) storyIdByWorkItemId[row.AzureDevOpsWorkItemId] = story.Id;
            if (row.GitHubIssueKey is not null) storyIdByGitHubKey[row.GitHubIssueKey] = story.Id;
        }

        return new ApplyResult(createdCount, updatedCount, storyIdByJiraKey, storyIdByWorkItemId, storyIdByGitHubKey);
    }
}
