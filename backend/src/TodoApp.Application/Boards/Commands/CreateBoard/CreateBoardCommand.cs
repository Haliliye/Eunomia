using MediatR;
using TodoApp.Application.Boards.DTOs;

namespace TodoApp.Application.Boards.Commands.CreateBoard;

public record CreateBoardCommand(string TeamId, string Name, string? SprintId, string RequestingUserId) : IRequest<BoardDto>;
