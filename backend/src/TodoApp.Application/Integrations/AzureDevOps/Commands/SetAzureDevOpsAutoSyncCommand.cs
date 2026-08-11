using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record SetAzureDevOpsAutoSyncCommand(string TeamId, string RequestingUserId, bool Enabled) : IRequest;
