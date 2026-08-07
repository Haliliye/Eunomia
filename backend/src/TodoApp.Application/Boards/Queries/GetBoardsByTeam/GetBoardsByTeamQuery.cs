using MediatR;
using TodoApp.Application.Boards.DTOs;

namespace TodoApp.Application.Boards.Queries.GetBoardsByTeam;

public record GetBoardsByTeamQuery(string TeamId, string RequestingUserId) : IRequest<IReadOnlyList<BoardDto>>;
