using MediatR;

namespace TodoApp.Application.Teams.Commands.ReorderBoardColumns;

public record ReorderBoardColumnsCommand(string TeamId, IReadOnlyList<string> OrderedColumnKeys, string RequestingUserId) : IRequest;
