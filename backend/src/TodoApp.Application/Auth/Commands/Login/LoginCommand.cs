using MediatR;
using TodoApp.Application.Auth.DTOs;

namespace TodoApp.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
