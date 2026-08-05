using MediatR;

namespace TodoApp.Application.Integrations.Jira.Commands;

/// <summary>Step 1 of connecting Jira: returns the authorize.atlassian.com URL the frontend redirects the whole page to (this can't be an XHR — Atlassian's login/consent screen has to run in the top-level browser window).</summary>
public record StartJiraConnectionCommand(string RequestingUserId) : IRequest<string>;
