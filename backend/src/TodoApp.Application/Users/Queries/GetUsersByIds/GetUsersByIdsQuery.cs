using MediatR;
using TodoApp.Application.Users.DTOs;

namespace TodoApp.Application.Users.Queries.GetUsersByIds;

public record GetUsersByIdsQuery(IReadOnlyCollection<string> Ids) : IRequest<IReadOnlyList<UserSummaryDto>>;
