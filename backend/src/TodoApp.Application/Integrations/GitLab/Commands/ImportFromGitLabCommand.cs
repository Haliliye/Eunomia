using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.GitLab.Commands;

public record ImportFromGitLabCommand(string TeamId, string RequestingUserId, int ProjectId, string PathWithNamespace) : IRequest<ImportSummaryDto>;
