using Moq;
using TodoApp.Application.Auth.Commands.RefreshToken;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;
using Xunit;

namespace TodoApp.UnitTests.Auth;

/// <summary>
/// Coverage for refresh token rotation + reuse detection (family
/// revocation) — added alongside the P2 security items from the
/// 2026-08-11 review. Previously, rotation happened but a revoked token
/// presented again was treated identically to a genuinely expired one: no
/// signal was raised, and the rest of that token's family stayed valid.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private static User CreateUser() => User.Create(Guid.NewGuid().ToString(), "person@example.com", "Person", "hashed-password");

    private static (Mock<IRefreshTokenRepository> RefreshTokens, Mock<IUserRepository> Users, Mock<IJwtTokenGenerator> Jwt, RefreshTokenCommandHandler Handler) BuildHandler()
    {
        var refreshTokens = new Mock<IRefreshTokenRepository>();
        var users = new Mock<IUserRepository>();
        var jwt = new Mock<IJwtTokenGenerator>();
        var handler = new RefreshTokenCommandHandler(refreshTokens.Object, users.Object, jwt.Object);
        return (refreshTokens, users, jwt, handler);
    }

    [Fact]
    public async Task Handle_ActiveToken_RotatesAndCarriesTheSameFamilyIdForward()
    {
        var (refreshTokens, users, jwt, handler) = BuildHandler();
        var user = CreateUser();
        var rawToken = "some-raw-refresh-token";
        var existing = Domain.Auth.RefreshToken.Create(Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(rawToken), DateTime.UtcNow.AddDays(30), familyId: "family-1");

        refreshTokens.Setup(r => r.GetByTokenHashAsync(TokenHasher.Hash(rawToken), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        jwt.Setup(j => j.GenerateToken(user)).Returns("new-access-token");
        jwt.Setup(j => j.GenerateRefreshToken()).Returns(("new-raw-refresh-token", DateTime.UtcNow.AddDays(30)));

        Domain.Auth.RefreshToken? addedToken = null;
        refreshTokens.Setup(r => r.AddAsync(It.IsAny<Domain.Auth.RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Auth.RefreshToken, CancellationToken>((token, _) => addedToken = token);

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.Equal("new-access-token", result.Token);
        Assert.True(existing.RevokedOn is not null); // the used token itself is now revoked
        Assert.NotNull(addedToken);
        Assert.Equal("family-1", addedToken!.FamilyId); // family carried forward, not reset
        refreshTokens.Verify(r => r.RevokeAllInFamilyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_RevokesTheWholeFamilyAndThrows()
    {
        var (refreshTokens, users, jwt, handler) = BuildHandler();
        var user = CreateUser();
        var rawToken = "a-stolen-or-replayed-token";
        var alreadyRevoked = Domain.Auth.RefreshToken.Create(Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(rawToken), DateTime.UtcNow.AddDays(30), familyId: "family-2");
        alreadyRevoked.Revoke();

        refreshTokens.Setup(r => r.GetByTokenHashAsync(TokenHasher.Hash(rawToken), It.IsAny<CancellationToken>())).ReturnsAsync(alreadyRevoked);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None));

        refreshTokens.Verify(r => r.RevokeAllInFamilyAsync("family-2", It.IsAny<CancellationToken>()), Times.Once);
        // No new token should ever be issued in the reuse case.
        refreshTokens.Verify(r => r.AddAsync(It.IsAny<Domain.Auth.RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsWithoutTouchingAnyFamily()
    {
        var (refreshTokens, users, jwt, handler) = BuildHandler();
        refreshTokens.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Auth.RefreshToken?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new RefreshTokenCommand("never-issued"), CancellationToken.None));

        refreshTokens.Verify(r => r.RevokeAllInFamilyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredButNotRevokedToken_ThrowsWithoutTouchingAnyFamily()
    {
        // An honestly-expired token (never rotated, never reused) is not
        // reuse — no family-wide revocation should fire for it.
        var (refreshTokens, users, jwt, handler) = BuildHandler();
        var user = CreateUser();
        var rawToken = "an-expired-token";
        var expired = Domain.Auth.RefreshToken.Create(Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(rawToken), DateTime.UtcNow.AddDays(-1), familyId: "family-3");

        refreshTokens.Setup(r => r.GetByTokenHashAsync(TokenHasher.Hash(rawToken), It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None));

        refreshTokens.Verify(r => r.RevokeAllInFamilyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
