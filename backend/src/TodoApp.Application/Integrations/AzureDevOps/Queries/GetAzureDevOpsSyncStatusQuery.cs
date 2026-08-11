using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public record GetAzureDevOpsSyncStatusQuery(string TeamId, string RequestingUserId) : IRequest<AzureDevOpsSyncStatusDto>;

public record AzureDevOpsSyncStatusDto(bool IsLinked, string? ProjectName, bool AutoSyncEnabled, DateTime? LastSyncedOn);
