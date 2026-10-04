using Microsoft.JSInterop;
using PokedexTracker.Core;

namespace PokedexTracker.Services;

// Providers own authentication, file enumeration and conditional commit semantics.
// Folder commits preserve concurrent versions; arbitrary external edits cannot be atomic.
public interface ITrackerStorageProvider
{
    string Name { get; }
    bool SupportsAtomicReplacement { get; }
    Task<List<StorageFile>> ListAsync();
    Task<List<StorageFile>> CommitAsync(Guid id, List<StorageFile> expected, string? fileName, string? content);
}

public sealed record StorageFile(string FileName, string Content, string Hash, bool IntegrityValid, string? FilenameId, string? Error = null)
{
    public StoredTracker Stored => new(FileName, Content, Hash, IntegrityValid);
}

public sealed class FolderStorageProvider(IJSObjectReference browser) : ITrackerStorageProvider
{
    public string Name => "Folder";
    public bool SupportsAtomicReplacement => false;
    public Task<List<StorageFile>> ListAsync() => browser.InvokeAsync<List<StorageFile>>("listFolder").AsTask();
    public Task<List<StorageFile>> CommitAsync(Guid id, List<StorageFile> expected, string? fileName, string? content) =>
        browser.InvokeAsync<List<StorageFile>>("commitFolder", id.ToString("N"), expected, fileName, content).AsTask();
}

public sealed record BrowserCapabilities(bool Folder, bool Online, bool Persistent);
public sealed record TrackerConflict(Guid TrackerId, string Reason, List<StorageFile> Candidates);
