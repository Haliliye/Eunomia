using MediatR;

namespace TodoApp.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
