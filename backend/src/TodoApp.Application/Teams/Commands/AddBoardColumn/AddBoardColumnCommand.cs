using MediatR;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Commands.AddBoardColumn;

public record AddBoardColumnCommand(string TeamId, string Name, string RequestingUserId) : IRequest<BoardColumnDto>;
