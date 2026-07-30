using MediatR;
using TodoApp.Application.Auth.DTOs;

namespace TodoApp.Application.Auth.Commands.Register;

public record RegisterCommand(string Email, string DisplayName, string Password) : IRequest<AuthResultDto>;
