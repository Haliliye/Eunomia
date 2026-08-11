using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public record ImportFromAzureDevOpsCommand(string TeamId, string RequestingUserId, string ProjectName) : IRequest<ImportSummaryDto>;
