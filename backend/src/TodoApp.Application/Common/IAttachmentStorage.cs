namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction over where attachment bytes actually live — implemented as
/// local disk storage in this skeleton (see LocalDiskAttachmentStorage).
/// A real multi-instance deployment would swap this for S3/Azure Blob/etc.
/// without touching Application or Domain at all.
/// </summary>
public interface IAttachmentStorage
{
    /// <summary>Saves the content and returns a storage key — an opaque string
    /// this same implementation can later use to find the file again. Never
    /// derived from the original filename (avoids path-traversal/collision issues).</summary>
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
