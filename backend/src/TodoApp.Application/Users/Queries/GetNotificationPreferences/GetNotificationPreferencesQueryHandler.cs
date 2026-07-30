using MediatR;
using TodoApp.Application.Users.DTOs;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Users.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    private readonly IUserRepository _userRepository;

    public GetNotificationPreferencesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferencesDto> Handle(GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return new NotificationPreferencesDto(user.NotifyOnAssignment, user.NotifyOnMention, user.NotifyOnInvitation, user.NotifyOnDueSoon, user.ReminderLeadTimeHours);
    }
}
