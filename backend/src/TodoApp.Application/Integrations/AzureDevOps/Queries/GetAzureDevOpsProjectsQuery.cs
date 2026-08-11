using MediatR;
using TodoApp.Application.Common;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public record GetAzureDevOpsProjectsQuery(string RequestingUserId) : IRequest<IReadOnlyList<AzureDevOpsProjectDto>>;
