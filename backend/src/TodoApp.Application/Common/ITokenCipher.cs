namespace TodoApp.Application.Common;

/// <summary>
/// Symmetric encrypt/decrypt for secrets stored at rest (Jira access/refresh
/// tokens). Deliberately NOT ASP.NET Core's Data Protection API: that keys
/// itself from a key ring persisted to local disk by default, which doesn't
/// survive a redeploy on Render's free tier (ephemeral filesystem — same
/// issue that motivated R2 for attachments) — every deploy would silently
/// make all previously-stored tokens undecryptable. This uses a fixed key
/// from configuration instead (Jira:TokenEncryptionKey), same pattern as
/// Jwt:SecretKey — stable across restarts and deploys.
/// </summary>
public interface ITokenCipher
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
