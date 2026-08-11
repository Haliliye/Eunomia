using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record ConnectAzureDevOpsCommand(string RequestingUserId, string OrganizationName, string PersonalAccessToken) : IRequest<ConnectAzureDevOpsResult>;

public record ConnectAzureDevOpsResult(bool Success, string? ErrorMessage);
