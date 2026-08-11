using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record DisconnectAzureDevOpsCommand(string RequestingUserId) : IRequest;
