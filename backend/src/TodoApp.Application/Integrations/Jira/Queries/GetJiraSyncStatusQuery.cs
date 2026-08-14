using MediatR;
using TodoApp.Application.Integrations;

namespace TodoApp.Application.Integrations.Jira.Queries;

public record GetJiraSyncStatusQuery(string TeamId, string RequestingUserId) : IRequest<JiraSyncStatusDto>;

public record JiraSyncStatusDto(bool IsLinked, string? ProjectKey, bool AutoSyncEnabled, DateTime? LastSyncedOn, IReadOnlyList<SyncLogEntryDto>? History = null);
