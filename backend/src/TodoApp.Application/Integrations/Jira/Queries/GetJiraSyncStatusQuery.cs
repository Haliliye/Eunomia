using MediatR;

namespace TodoApp.Application.Integrations.Jira.Queries;

public record GetJiraSyncStatusQuery(string TeamId, string RequestingUserId) : IRequest<JiraSyncStatusDto>;

public record JiraSyncStatusDto(bool IsLinked, string? ProjectKey, bool AutoSyncEnabled, DateTime? LastSyncedOn);
