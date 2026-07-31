namespace TodoApp.Infrastructure.Attachments;

/// <summary>
/// Cloudflare R2 is S3-compatible, so this reuses the AWS S3 SDK pointed at
/// R2's endpoint instead of AWS's — no egress fees on R2, which matters for
/// a free-tier deployment (S3 itself charges for egress even within its own
/// free tier's request allowance).
/// </summary>
public class R2StorageSettings
{
    public const string SectionName = "R2Storage";

    /// <summary>Whether R2 is actually configured — if false (or this whole
    /// section is absent), DependencyInjection falls back to local disk
    /// storage instead. Lets local dev work without R2 credentials at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>https://&lt;account-id&gt;.r2.cloudflarestorage.com — from the R2 dashboard.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}
