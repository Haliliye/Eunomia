using MediatR;

namespace TodoApp.Application.Teams.Commands.RenameBoardColumn;

public record RenameBoardColumnCommand(string TeamId, string ColumnKey, string Name, string RequestingUserId) : IRequest;
