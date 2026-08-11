using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record StartAzureDevOpsConnectionCommand(string RequestingUserId) : IRequest<string>;
