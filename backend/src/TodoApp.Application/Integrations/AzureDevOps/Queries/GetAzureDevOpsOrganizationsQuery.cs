using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public record GetAzureDevOpsOrganizationsQuery(string RequestingUserId) : IRequest<IReadOnlyList<string>>;
