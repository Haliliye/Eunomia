using MediatR;

namespace TodoApp.Application.Boards.Commands.RenameBoard;

public record RenameBoardCommand(string BoardId, string Name, string? SprintId, string RequestingUserId) : IRequest;
