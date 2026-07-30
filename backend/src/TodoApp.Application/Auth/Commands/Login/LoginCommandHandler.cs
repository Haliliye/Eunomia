using MediatR;
using TodoApp.Application.Auth.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Deliberately the same error for "no such user" and "wrong password" —
        // distinguishing them lets an attacker enumerate registered emails.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationFailedException("Invalid email or password.");

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var (refreshToken, expiresOn) = _jwtTokenGenerator.GenerateRefreshToken();

        // A fresh RefreshToken row per login — each device/browser gets its
        // own, so logging in on a second device doesn't invalidate the first.
        var refreshTokenEntity = Domain.Auth.RefreshToken.Create(Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(refreshToken), expiresOn);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return new AuthResultDto(accessToken, refreshToken, user.Id, user.Email, user.DisplayName, user.IsEmailVerified);
    }
}
