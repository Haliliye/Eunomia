using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository resetTokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _resetTokenRepository = resetTokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.Token);
        var resetToken = await _resetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsActive)
            throw new AuthenticationFailedException("This password reset link is invalid or has expired. Request a new one.");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("This password reset link is invalid or has expired. Request a new one.");

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        await _userRepository.UpdateAsync(user, cancellationToken);

        resetToken.MarkUsed();
        await _resetTokenRepository.UpdateAsync(resetToken, cancellationToken);
    }
}
