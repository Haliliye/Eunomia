using MediatR;

namespace TodoApp.Application.Auth.Commands.RequestPasswordReset;

/// <summary>Returns the raw token ONLY in Development (see handler) — in a
/// real deployment this would be emailed instead, never returned in the response.</summary>
public record RequestPasswordResetCommand(string Email) : IRequest<string?>;
