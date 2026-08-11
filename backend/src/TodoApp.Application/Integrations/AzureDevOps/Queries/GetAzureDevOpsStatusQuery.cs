using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public record GetAzureDevOpsStatusQuery(string RequestingUserId) : IRequest<AzureDevOpsStatusDto>;

public record AzureDevOpsStatusDto(bool IsConnected, string? OrganizationName, DateTime? ConnectedOn);
