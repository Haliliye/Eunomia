using MediatR;

namespace TodoApp.Application.Integrations.GitHub.Commands;

/// <summary>Step 1 of connecting GitHub: returns the github.com/login/oauth/authorize URL the frontend redirects the whole page to.</summary>
public record StartGitHubConnectionCommand(string RequestingUserId) : IRequest<string>;
