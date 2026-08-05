using MediatR;

namespace TodoApp.Application.Integrations.Jira.Commands;

/// <summary>Step 2: Atlassian redirects the browser here with a one-time code and the state we handed it in step 1.</summary>
public record CompleteJiraConnectionCommand(string Code, string State) : IRequest<CompleteJiraConnectionResult>;

public record CompleteJiraConnectionResult(bool Success, string? SiteName, string? ErrorMessage);
