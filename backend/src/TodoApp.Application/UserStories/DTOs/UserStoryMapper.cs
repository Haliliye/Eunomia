using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.DTOs;

internal static class UserStoryMapper
{
    public static UserStoryDto ToDto(UserStory story) => new(
        story.Id,
        story.TeamId,
        story.Title,
        story.Description,
        story.Status,
        story.Priority.ToString(),
        story.AssigneeId,
        story.DueDate,
        story.CreatedOn,
        story.Version,
        story.IsArchived,
        story.StoryPoints,
        story.SprintId,
        story.ChecklistItems.Select(i => new ChecklistItemDto(i.Id, i.Text, i.IsCompleted, i.Order)).ToList(),
        story.LabelIds.ToList(),
        story.RecurrenceFrequency?.ToString(),
        story.RecurrenceEndDate,
        story.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedByUserId, a.UploadedOn)).ToList(),
        story.EstimatedHours,
        story.TimeLogEntries.Select(t => new TimeLogEntryDto(t.Id, t.Hours, t.Note, t.LoggedByUserId, t.LoggedOn)).ToList(),
        story.TotalLoggedHours,
        story.Links.Select(l => new StoryLinkDto(l.LinkedStoryId, l.LinkType.ToString())).ToList(),
        story.CreatedByUserId,
        story.ParentId,
        story.EpicId);
}
