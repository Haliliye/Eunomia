using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Security;

public class TokenEncryptionSettings
{
    public const string SectionName = "TokenEncryption";

    /// <summary>Base64-encoded 32-byte (256-bit) key. Generate once with e.g. `openssl rand -base64 32` and keep it stable — rotating it makes every previously-stored token undecryptable.</summary>
    public string Key { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Key);
}

/// <summary>AES-256-GCM: authenticated encryption, so a tampered ciphertext fails to decrypt rather than silently returning garbage.</summary>
public class AesTokenCipher : ITokenCipher
{
    private const int NonceSize = 12; // 96-bit nonce, the standard/recommended size for GCM
    private const int TagSize = 16; // 128-bit authentication tag

    private readonly byte[] _key;

    public AesTokenCipher(IOptions<TokenEncryptionSettings> settings)
    {
        if (!settings.Value.IsConfigured)
            throw new InvalidOperationException(
                "TokenEncryption:Key is not configured. Generate one with `openssl rand -base64 32` and set it (Jira integration needs it to store tokens securely).");

        _key = Convert.FromBase64String(settings.Value.Key);
        if (_key.Length != 32)
            throw new InvalidOperationException("TokenEncryption:Key must decode to exactly 32 bytes (256 bits).");
    }

    public string Encrypt(string plainText)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Layout: nonce || tag || ciphertext — all we need to decrypt later, self-contained.
        var combined = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, combined, NonceSize + TagSize, cipherBytes.Length);

        // URL-safe base64 (RFC 4648 §5): state travels through a redirect
        // chain we don't control (our redirect_uri -> Atlassian -> back to
        // us), so avoid the standard alphabet's '+', '/', '=' entirely
        // rather than trust every hop to preserve them byte-for-byte.
        return ToBase64Url(combined);
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }

    public string Decrypt(string cipherText)
    {
        var combined = FromBase64Url(cipherText);
        var nonce = combined[..NonceSize];
        var tag = combined[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = combined[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
