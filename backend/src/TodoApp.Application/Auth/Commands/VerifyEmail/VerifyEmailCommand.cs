using MediatR;

namespace TodoApp.Application.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest;
