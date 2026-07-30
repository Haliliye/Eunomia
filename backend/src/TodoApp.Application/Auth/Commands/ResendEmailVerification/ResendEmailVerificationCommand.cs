using MediatR;

namespace TodoApp.Application.Auth.Commands.ResendEmailVerification;

/// <summary>Returns the raw token ONLY in Development — see VerifyEmail's registration-time note.</summary>
public record ResendEmailVerificationCommand(string UserId) : IRequest<string?>;
