using MediatR;

namespace TodoApp.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
