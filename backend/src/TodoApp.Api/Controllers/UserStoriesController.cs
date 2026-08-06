using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Common;
using TodoApp.Application.UserStories.Commands.AddAttachment;
using TodoApp.Application.UserStories.Commands.AddChecklistItem;
using TodoApp.Application.UserStories.Commands.AddStoryLink;
using TodoApp.Application.UserStories.Commands.AddLabelToUserStory;
using TodoApp.Application.UserStories.Commands.ArchiveUserStory;
using TodoApp.Application.UserStories.Commands.AssignUserStory;
using TodoApp.Application.UserStories.Commands.ChangePriority;
using TodoApp.Application.UserStories.Commands.ChangeStatus;
using TodoApp.Application.UserStories.Commands.CreateUserStory;
using TodoApp.Application.UserStories.Commands.CreateSubtask;
using TodoApp.Application.UserStories.Commands.DeleteUserStory;
using TodoApp.Application.UserStories.Commands.MoveUserStoryToSprint;
using TodoApp.Application.UserStories.Commands.RemoveAttachment;
using TodoApp.Application.UserStories.Commands.RemoveChecklistItem;
using TodoApp.Application.UserStories.Commands.RemoveStoryLink;
using TodoApp.Application.UserStories.Commands.RemoveLabelFromUserStory;
using TodoApp.Application.UserStories.Commands.ReorderChecklistItems;
using TodoApp.Application.UserStories.Commands.LogTime;
using TodoApp.Application.UserStories.Commands.SetEstimate;
using TodoApp.Application.UserStories.Commands.SetRecurrence;
using TodoApp.Application.UserStories.Commands.AnalyzeCsv;
using TodoApp.Application.UserStories.Commands.BulkCreateUserStories;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Application.UserStories.Queries.ExportUserStories;
using TodoApp.Application.UserStories.Queries.GetAttachmentDownload;
using TodoApp.Application.UserStories.Queries.GetStoryLinks;
using TodoApp.Application.UserStories.Queries.GetSubtasks;
using TodoApp.Application.UserStories.Commands.ToggleChecklistItem;
using TodoApp.Application.UserStories.Commands.UnarchiveUserStory;
using TodoApp.Application.UserStories.Commands.UpdateUserStory;
using TodoApp.Application.Activities.Queries.GetUserStoryActivity;
using TodoApp.Application.UserStories.Queries.GetTeamDashboard;
using TodoApp.Application.UserStories.Queries.GetUserStoriesByTeam;
using TodoApp.Application.UserStories.Queries.GetUserStoryById;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/userstories")]
public class UserStoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserStoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetByTeam(
        [FromQuery] string teamId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? assigneeId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool showArchived = false,
        [FromQuery] string? sprintId = null,
        [FromQuery] string? labelId = null,
        CancellationToken cancellationToken = default)
    {
        var stories = await _mediator.Send(
            new GetUserStoriesByTeamQuery(teamId, status, priority, assigneeId, keyword, page, pageSize, showArchived, sprintId, labelId),
            cancellationToken);

        return Ok(stories);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string teamId, [FromQuery] string? sprintId, CancellationToken cancellationToken)
    {
        var dashboard = await _mediator.Send(new GetTeamDashboardQuery(teamId, User.GetUserId(), sprintId), cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var story = await _mediator.Send(new GetUserStoryByIdQuery(id, User.GetUserId()), cancellationToken);
        return story is null ? NotFound() : Ok(story);
    }

    /// <summary>US-131: this story's own activity history.</summary>
    [HttpGet("{id}/activity")]
    public async Task<IActionResult> GetActivity(string id, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var activity = await _mediator.Send(new GetUserStoryActivityQuery(id, User.GetUserId(), limit), cancellationToken);
        return Ok(activity);
    }

    /// <summary>Linked stories ("Blocks"/"BlockedBy"/"RelatesTo") — classic Jira-style issue linking.</summary>
    [HttpGet("{id}/links")]
    public async Task<IActionResult> GetLinks(string id, CancellationToken cancellationToken)
    {
        var links = await _mediator.Send(new GetStoryLinksQuery(id, User.GetUserId()), cancellationToken);
        return Ok(links);
    }

    [HttpPost("{id}/links")]
    public async Task<IActionResult> AddLink(string id, [FromBody] AddStoryLinkRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddStoryLinkCommand(id, request.LinkedStoryId, request.LinkType, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    /// <summary>This story's subtasks — see UserStory.ParentId. Deliberately excluded from the normal backlog/board listing (SearchAsync), same as Jira.</summary>
    [HttpGet("{id}/subtasks")]
    public async Task<IActionResult> GetSubtasks(string id, CancellationToken cancellationToken)
    {
        var subtasks = await _mediator.Send(new GetSubtasksQuery(id, User.GetUserId()), cancellationToken);
        return Ok(subtasks);
    }

    [HttpPost("{id}/subtasks")]
    public async Task<IActionResult> CreateSubtask(string id, [FromBody] CreateSubtaskRequest request, CancellationToken cancellationToken)
    {
        var subtask = await _mediator.Send(new CreateSubtaskCommand(id, request.Title, User.GetUserId()), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = subtask.Id }, subtask);
    }

    public record CreateSubtaskRequest(string Title);

    [HttpDelete("{id}/links/{linkedStoryId}")]
    public async Task<IActionResult> RemoveLink(string id, string linkedStoryId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveStoryLinkCommand(id, linkedStoryId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserStoryRequest request, CancellationToken cancellationToken)
    {
        var story = await _mediator.Send(
            new CreateUserStoryCommand(request.TeamId, request.Title, request.Description, User.GetUserId()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = story.Id }, story);
    }

    /// <summary>Each line of the pasted text becomes one story.</summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateUserStoriesRequest request, CancellationToken cancellationToken)
    {
        var stories = await _mediator.Send(new BulkCreateUserStoriesCommand(request.TeamId, request.Titles, User.GetUserId()), cancellationToken);
        return Ok(stories);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserStoryRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateUserStoryCommand(id, request.Title, request.Description, request.DueDate, request.StoryPoints, request.ExpectedVersion, User.GetUserId()),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserStoryCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ChangeUserStoryStatusCommand(id, request.Status, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/priority")]
    public async Task<IActionResult> ChangePriority(string id, [FromBody] ChangePriorityRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ChangeUserStoryPriorityCommand(id, request.Priority, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/assignee")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignUserStoryRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignUserStoryCommand(id, request.AssigneeId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/archive")]
    public async Task<IActionResult> Archive(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveUserStoryCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/unarchive")]
    public async Task<IActionResult> Unarchive(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnarchiveUserStoryCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/sprint")]
    public async Task<IActionResult> MoveToSprint(string id, [FromBody] MoveToSprintRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MoveUserStoryToSprintCommand(id, request.SprintId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/checklist-items")]
    public async Task<IActionResult> AddChecklistItem(string id, [FromBody] AddChecklistItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new AddChecklistItemCommand(id, request.Text, User.GetUserId()), cancellationToken);
        return Ok(item);
    }

    [HttpPut("{id}/checklist-items/{itemId}/toggle")]
    public async Task<IActionResult> ToggleChecklistItem(string id, string itemId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ToggleChecklistItemCommand(id, itemId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/checklist-items/{itemId}")]
    public async Task<IActionResult> RemoveChecklistItem(string id, string itemId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveChecklistItemCommand(id, itemId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/checklist-items/reorder")]
    public async Task<IActionResult> ReorderChecklistItems(string id, [FromBody] ReorderChecklistItemsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderChecklistItemsCommand(id, request.OrderedItemIds, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/labels/{labelId}")]
    public async Task<IActionResult> AddLabel(string id, string labelId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddLabelToUserStoryCommand(id, labelId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/labels/{labelId}")]
    public async Task<IActionResult> RemoveLabel(string id, string labelId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveLabelFromUserStoryCommand(id, labelId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/recurrence")]
    public async Task<IActionResult> SetRecurrence(string id, [FromBody] SetRecurrenceRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetRecurrenceCommand(id, request.Frequency, request.EndDate, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(11 * 1024 * 1024)] // a little headroom over the 10 MB domain limit so the rejection is our own clear error, not IIS/Kestrel's generic one
    public async Task<IActionResult> AddAttachment(string id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var attachment = await _mediator.Send(
            new AddAttachmentCommand(id, file.FileName, file.ContentType, file.Length, stream, User.GetUserId()),
            cancellationToken);

        return Ok(attachment);
    }

    /// <summary>Images/PDFs render inline (US-135's "preview" case); everything else forces a download.</summary>
    [HttpGet("{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(string id, string attachmentId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAttachmentDownloadQuery(id, attachmentId, User.GetUserId()), cancellationToken);

        var isPreviewable = result.ContentType.StartsWith("image/") || result.ContentType == "application/pdf";
        return isPreviewable
            ? File(result.Content, result.ContentType)
            : File(result.Content, result.ContentType, result.FileName);
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> RemoveAttachment(string id, string attachmentId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveAttachmentCommand(id, attachmentId, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/estimate")]
    public async Task<IActionResult> SetEstimate(string id, [FromBody] SetEstimateRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetEstimateCommand(id, request.Hours, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/time-logs")]
    public async Task<IActionResult> LogTime(string id, [FromBody] LogTimeRequest request, CancellationToken cancellationToken)
    {
        var entry = await _mediator.Send(new LogTimeCommand(id, request.Hours, request.Note, User.GetUserId()), cancellationToken);
        return Ok(entry);
    }

    /// <summary>US-146: exports whatever the caller is currently filtered by — the frontend passes the same query params it's using for the backlog view.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string teamId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? assigneeId,
        [FromQuery] string? keyword,
        [FromQuery] string? sprintId,
        [FromQuery] string? labelId,
        [FromQuery] bool showArchived,
        CancellationToken cancellationToken)
    {
        var csv = await _mediator.Send(
            new ExportUserStoriesQuery(teamId, User.GetUserId(), status, priority, assigneeId, keyword, sprintId, labelId, showArchived),
            cancellationToken);

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"stories-export-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    /// <summary>US-147: parses and validates only — nothing is created yet, so the caller can review the mapping/errors first.</summary>
    /// <summary>Step 1 of importing any CSV (Jira export, Azure DevOps export, or our own) — just reads the header row + a sample, no mapping applied yet.</summary>
    [HttpPost("import/analyze")]
    public async Task<IActionResult> AnalyzeCsv([FromQuery] string teamId, IFormFile file, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);

        var analysis = await _mediator.Send(new AnalyzeCsvCommand(teamId, User.GetUserId(), content), cancellationToken);
        return Ok(analysis);
    }

    [HttpPost("import/preview")]
    public async Task<IActionResult> PreviewImport([FromQuery] string teamId, IFormFile file, [FromForm] string mapping, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);
        var columnMapping = DeserializeMapping(mapping);

        var rows = await _mediator.Send(new PreviewImportUserStoriesCommand(teamId, User.GetUserId(), content, columnMapping), cancellationToken);
        return Ok(rows);
    }

    [HttpPost("import/confirm")]
    public async Task<IActionResult> ConfirmImport([FromQuery] string teamId, IFormFile file, [FromForm] string mapping, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);
        var columnMapping = DeserializeMapping(mapping);

        var summary = await _mediator.Send(new ImportUserStoriesCommand(teamId, User.GetUserId(), content, columnMapping), cancellationToken);
        return Ok(summary);
    }

    /// <summary>The mapping travels as a JSON-encoded form field alongside the
    /// file (multipart/form-data can't easily carry a nested JSON body and a
    /// file in the same [FromBody]) — System.Text.Json's default naming
    /// policy expects camelCase, matching what the frontend sends.</summary>
    private static CsvColumnMapping DeserializeMapping(string mappingJson)
    {
        return System.Text.Json.JsonSerializer.Deserialize<CsvColumnMapping>(mappingJson, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new ArgumentException("Invalid column mapping.");
    }
}

public record CreateUserStoryRequest(string TeamId, string Title, string? Description);
public record BulkCreateUserStoriesRequest(string TeamId, List<string> Titles);
public record UpdateUserStoryRequest(string Title, string? Description, DateTime? DueDate, int? StoryPoints, int ExpectedVersion);
public record ChangeStatusRequest(string Status);
public record ChangePriorityRequest(string Priority);
public record AssignUserStoryRequest(string? AssigneeId);
public record MoveToSprintRequest(string? SprintId);
public record AddChecklistItemRequest(string Text);
public record ReorderChecklistItemsRequest(List<string> OrderedItemIds);
public record SetRecurrenceRequest(string? Frequency, DateTime? EndDate);
public record SetEstimateRequest(double? Hours);
public record AddStoryLinkRequest(string LinkedStoryId, string LinkType);
public record LogTimeRequest(double Hours, string? Note);
