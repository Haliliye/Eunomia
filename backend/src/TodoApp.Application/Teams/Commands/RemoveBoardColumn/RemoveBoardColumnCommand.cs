using MediatR;

namespace TodoApp.Application.Teams.Commands.RemoveBoardColumn;

public record RemoveBoardColumnCommand(string TeamId, string ColumnKey, string RequestingUserId) : IRequest;
