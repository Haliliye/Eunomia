using MediatR;
using TodoApp.Application.Auth.DTOs;

namespace TodoApp.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;
