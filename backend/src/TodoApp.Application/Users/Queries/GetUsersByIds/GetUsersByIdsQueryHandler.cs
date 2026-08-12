using MediatR;
using TodoApp.Application.Users.DTOs;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Users.Queries.GetUsersByIds;

public class GetUsersByIdsQueryHandler : IRequestHandler<GetUsersByIdsQuery, IReadOnlyList<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByIdsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> Handle(GetUsersByIdsQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByIdsAsync(request.Ids, cancellationToken);
        return users.Select(u => new UserSummaryDto(u.Id, u.DisplayName)).ToList();
    }
}
