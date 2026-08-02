using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Attachments;

/// <summary>
/// Cloudflare R2-backed IAttachmentStorage — swapped in for
/// LocalDiskAttachmentStorage when R2Storage:Enabled is true (see
/// DependencyInjection). This is exactly the swap the original local-disk
/// implementation's docs called out as needed for any real multi-instance
/// or ephemeral-filesystem deployment (e.g. Render's free tier, whose disk
/// doesn't survive a redeploy).
///
/// Storage keys stay the same opaque, self-generated GUIDs as the local-disk
/// version — they become the R2 object key directly, never derived from the
/// original filename.
/// </summary>
public class R2AttachmentStorage : IAttachmentStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public R2AttachmentStorage(IAmazonS3 s3Client, IOptions<R2StorageSettings> settings)
    {
        _s3Client = s3Client;
        _bucketName = settings.Value.BucketName;
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var storageKey = Guid.NewGuid().ToString("N");

        // A plain PutObjectRequest, not TransferUtility — TransferUtility
        // uploads using AWS's "streaming SigV4" (chunked payload signing),
        // which R2 doesn't implement at all ("STREAMING-AWS4-HMAC-SHA256-PAYLOAD
        // not implemented", confirmed as a known R2 limitation by Cloudflare's
        // own community). DisablePayloadSigning switches to the simpler
        // unsigned-payload variant R2 does support.
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = content,
            AutoCloseStream = false, // the caller (AddAttachmentCommandHandler) owns the stream's lifetime
            DisablePayloadSigning = true,
        }, cancellationToken);

        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey
        }, cancellationToken);

        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey
        }, cancellationToken);
    }
}
