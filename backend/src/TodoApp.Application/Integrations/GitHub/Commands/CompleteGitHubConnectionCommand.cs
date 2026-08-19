using MediatR;

namespace TodoApp.Application.Integrations.GitHub.Commands;

/// <summary>Step 2: GitHub redirects the browser here with a one-time code and the state we handed it in step 1.</summary>
public record CompleteGitHubConnectionCommand(string Code, string State) : IRequest<CompleteGitHubConnectionResult>;

public record CompleteGitHubConnectionResult(bool Success, string? GitHubLogin, string? ErrorMessage);
