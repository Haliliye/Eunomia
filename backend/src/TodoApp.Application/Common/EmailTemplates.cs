namespace TodoApp.Application.Common;

/// <summary>Plain, dependency-free HTML strings — no templating engine needed for two short transactional emails.</summary>
public static class EmailTemplates
{
    private const string Wrapper = """
        <div style="font-family: -apple-system, Segoe UI, Roboto, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px;">
          <h2 style="color: #0B6E63; margin-bottom: 4px;">Eunomia</h2>
          {0}
          <a href="{1}" style="display: inline-block; margin-top: 16px; padding: 10px 20px; background: #0B6E63; color: white; text-decoration: none; border-radius: 6px;">{2}</a>
          <p style="color: #888; font-size: 12px; margin-top: 24px;">If you didn't request this, you can safely ignore this email.</p>
        </div>
        """;

    public static string VerifyEmail(string verificationLink) => string.Format(
        Wrapper,
        "<p>Welcome! Click below to verify your email address.</p>",
        verificationLink,
        "Verify email");

    public static string ResetPassword(string resetLink) => string.Format(
        Wrapper,
        "<p>Someone requested a password reset for this account. This link expires in 1 hour.</p>",
        resetLink,
        "Reset password");

    public static string SignupInvitation(string teamName, string inviterName, string signupLink) => string.Format(
        Wrapper,
        $"<p>{inviterName} moved {teamName}'s work into Eunomia and included you as an assignee. " +
        $"Create an account with this same email address and you'll be added to {teamName} automatically.</p>",
        signupLink,
        "Create your account");
}
