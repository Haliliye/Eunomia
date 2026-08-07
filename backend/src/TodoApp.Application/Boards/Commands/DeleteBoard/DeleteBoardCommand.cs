using MediatR;

namespace TodoApp.Application.Boards.Commands.DeleteBoard;

public record DeleteBoardCommand(string BoardId, string RequestingUserId) : IRequest;
