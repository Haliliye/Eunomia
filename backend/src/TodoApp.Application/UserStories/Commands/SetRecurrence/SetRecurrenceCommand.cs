using MediatR;

namespace TodoApp.Application.UserStories.Commands.SetRecurrence;

/// <summary>Frequency null turns recurrence off (US-130).</summary>
public record SetRecurrenceCommand(string UserStoryId, string? Frequency, DateTime? EndDate, string RequestingUserId) : IRequest;
