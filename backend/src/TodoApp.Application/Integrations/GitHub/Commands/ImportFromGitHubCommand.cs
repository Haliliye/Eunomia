using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.GitHub.Commands;

public record ImportFromGitHubCommand(string TeamId, string RequestingUserId, string Owner, string Repo) : IRequest<ImportSummaryDto>;
