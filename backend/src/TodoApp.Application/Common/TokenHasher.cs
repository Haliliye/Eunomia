using System.Security.Cryptography;
using System.Text;

namespace TodoApp.Application.Common;

/// <summary>
/// Hashes refresh tokens before storage — same principle as password
/// hashing (never store the raw secret), but a fast hash is fine here since
/// the token itself is already high-entropy random data, not a guessable
/// password. Uses only BCL crypto (no external library), so this can live
/// in Application without an Infrastructure abstraction.
/// </summary>
public static class TokenHasher
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    public static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
