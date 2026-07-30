using MediatR;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Users.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateNotificationPreferencesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.UpdateNotificationPreferences(request.NotifyOnAssignment, request.NotifyOnMention, request.NotifyOnInvitation, request.NotifyOnDueSoon, request.ReminderLeadTimeHours);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
