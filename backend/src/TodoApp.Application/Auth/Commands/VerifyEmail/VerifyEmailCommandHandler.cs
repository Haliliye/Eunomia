using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;

    public VerifyEmailCommandHandler(IEmailVerificationTokenRepository tokenRepository, IUserRepository userRepository)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.Token);
        var token = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || !token.IsActive)
            throw new AuthenticationFailedException("This verification link is invalid or has expired. Request a new one.");

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("This verification link is invalid or has expired. Request a new one.");

        user.VerifyEmail();
        await _userRepository.UpdateAsync(user, cancellationToken);

        token.MarkUsed();
        await _tokenRepository.UpdateAsync(token, cancellationToken);
    }
}
