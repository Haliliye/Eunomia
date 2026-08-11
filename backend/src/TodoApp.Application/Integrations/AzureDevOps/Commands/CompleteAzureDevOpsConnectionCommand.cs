using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record CompleteAzureDevOpsConnectionCommand(string Code, string State) : IRequest<CompleteAzureDevOpsConnectionResult>;

public record CompleteAzureDevOpsConnectionResult(bool Success, string? ErrorMessage);
