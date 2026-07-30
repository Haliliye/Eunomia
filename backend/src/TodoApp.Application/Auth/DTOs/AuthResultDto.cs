namespace TodoApp.Application.Auth.DTOs;

public record AuthResultDto(
    string Token,
    string RefreshToken,
    string UserId,
    string Email,
    string DisplayName,
    bool IsEmailVerified,
    string? EmailVerificationDevToken = null);
