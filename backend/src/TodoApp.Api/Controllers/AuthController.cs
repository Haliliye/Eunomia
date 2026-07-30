using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TodoApp.Api.Common;
using TodoApp.Application.Auth.Commands.Login;
using TodoApp.Application.Auth.Commands.Logout;
using TodoApp.Application.Auth.Commands.RefreshToken;
using TodoApp.Application.Auth.Commands.Register;
using TodoApp.Application.Auth.Commands.RequestPasswordReset;
using TodoApp.Application.Auth.Commands.ResendEmailVerification;
using TodoApp.Application.Auth.Commands.ResetPassword;
using TodoApp.Application.Auth.Commands.VerifyEmail;
using TodoApp.Application.Auth.DTOs;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterCommand(request.Email, request.DisplayName, request.Password),
            cancellationToken);

        SetAuthCookies(result.Token, result.RefreshToken);
        return Ok(ToPublicResult(result));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        SetAuthCookies(result.Token, result.RefreshToken);
        return Ok(ToPublicResult(result));
    }

    /// <summary>Exchanges a still-valid refresh token for a new access token
    /// (and a new, rotated refresh token) — lets the frontend recover from an
    /// expired access token without the user having to log in again. The
    /// refresh token itself now comes from the httpOnly cookie, not the
    /// request body — the browser attaches it automatically.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "No refresh token cookie present." });

        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        SetAuthCookies(result.Token, result.RefreshToken);
        return Ok(ToPublicResult(result));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
            await _mediator.Send(new LogoutCommand(refreshToken), cancellationToken);

        ClearAuthCookies();
        return NoContent();
    }

    /// <summary>Always returns the same generic response whether or not the
    /// email is registered — telling the caller either way would let someone
    /// enumerate accounts. In Development only, the response also includes the
    /// raw reset token/link (no email server configured in this skeleton —
    /// see README's Auth section for how this would work with real email).</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var devToken = await _mediator.Send(new RequestPasswordResetCommand(request.Email), cancellationToken);

        return Ok(new
        {
            message = "If an account exists for that email, a password reset link has been sent.",
            devResetToken = devToken // null outside Development — see handler
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);
        return NoContent();
    }

    /// <summary>Anonymous — the person clicks this link from their (in this
    /// skeleton, dev-mode-surfaced) verification email before necessarily
    /// being logged in on that device.</summary>
    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new VerifyEmailCommand(request.Token), cancellationToken);
        return NoContent();
    }

    /// <summary>Requires auth — resends a verification link for the caller's own account.</summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(CancellationToken cancellationToken)
    {
        var devToken = await _mediator.Send(new ResendEmailVerificationCommand(User.GetUserId()), cancellationToken);
        return Ok(new { message = "If your email isn't verified yet, a new link has been sent.", devVerificationToken = devToken });
    }

    /// <summary>
    /// The raw JWT/refresh token never reaches the JS-accessible response
    /// body — only httpOnly cookies carry them. This is the whole point of
    /// the httpOnly migration: even a successful XSS payload running in the
    /// page can't read these values out of localStorage, because they're
    /// never put there in the first place.
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        // Based on THIS request's actual scheme, not an environment guess.
        //
        // Chrome's "schemeful same-site" policy treats http://localhost:5173
        // and https://localhost:5001 as CROSS-site (different scheme = different
        // site, even though the host is identical) — a SameSite=Lax cookie gets
        // silently dropped in that case, which is exactly the Visual Studio dev
        // setup (http frontend, https backend). SameSite=None fixes that, but
        // None requires Secure=true (browsers reject non-Secure None cookies).
        // Secure=true in turn requires the connection to actually be HTTPS —
        // which it will be whenever SameSite=None is chosen here, and won't be
        // for the Docker Compose setup (plain http both sides, same scheme, so
        // Lax already works fine there).
        var isSecure = Request.IsHttps;
        var sameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax;

        Response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure, // Secure cookies are dropped over plain http — off in dev so http://localhost keeps working
            SameSite = sameSite,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(15),
        });

        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSite,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
    }

    private void ClearAuthCookies()
    {
        // Path (and, per some browsers, SameSite/Secure) must match what the
        // cookie was originally set with, or the delete can silently miss it.
        var isSecure = Request.IsHttps;
        var sameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax;
        var options = new CookieOptions { Path = "/", Secure = isSecure, SameSite = sameSite };

        Response.Cookies.Delete(AccessTokenCookie, options);
        Response.Cookies.Delete(RefreshTokenCookie, options);
    }

    private static object ToPublicResult(AuthResultDto result) => new
    {
        result.UserId,
        result.Email,
        result.DisplayName,
        result.IsEmailVerified,
        result.EmailVerificationDevToken
    };
}

public record RegisterRequest(string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record VerifyEmailRequest(string Token);
