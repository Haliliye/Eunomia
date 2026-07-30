using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Attachments;

/// <summary>
/// Simplest possible IAttachmentStorage implementation — writes to a local
/// folder, keyed by a generated GUID (never the original filename, so two
/// uploads named the same thing can't collide and a crafted filename can't
/// path-traverse out of the storage folder).
/// </summary>
public class LocalDiskAttachmentStorage : IAttachmentStorage
{
    private readonly string _rootPath;

    public LocalDiskAttachmentStorage(IOptions<AttachmentStorageSettings> settings)
    {
        _rootPath = Path.GetFullPath(settings.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var storageKey = Guid.NewGuid().ToString("N");
        var path = Path.Combine(_rootPath, storageKey);

        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        // storageKey is always a GUID we generated ourselves (never derived from
        // user input) — GetFullPath + the "still under root" check is still worth
        // keeping as a defense-in-depth guard against a storageKey ever coming
        // from somewhere less trusted in the future.
        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!path.StartsWith(_rootPath, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");

        return path;
    }
}
