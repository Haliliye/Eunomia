using MediatR;
using TodoApp.Application.Auth.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
            throw new AuthenticationFailedException("Invalid or expired refresh token. Please log in again.");

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("Invalid or expired refresh token. Please log in again.");

        // Rotate: the used token is revoked and a new one issued in its place.
        // If a leaked/stale token gets used after rotation, it simply no
        // longer works — the caller must already have gotten a newer one.
        existingToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        var newAccessToken = _jwtTokenGenerator.GenerateToken(user);
        var (newRefreshToken, expiresOn) = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshTokenEntity = Domain.Auth.RefreshToken.Create(
            Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(newRefreshToken), expiresOn);
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        return new AuthResultDto(newAccessToken, newRefreshToken, user.Id, user.Email, user.DisplayName, user.IsEmailVerified);
    }
}
