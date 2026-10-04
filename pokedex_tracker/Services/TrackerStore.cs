using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using PokedexTracker.Core;

namespace PokedexTracker.Services;

public sealed class TrackerStore(HttpClient http, IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? browser;
    private ITrackerStorageProvider? provider;
    private readonly SemaphoreSlim operations = new(1, 1);
    public Catalog Catalog { get; private set; } = null!;
    public Dictionary<string, PokemonVariant> Pokemon { get; private set; } = [];
    public List<LocalTracker> Trackers { get; private set; } = [];
    public List<TrackerConflict> Conflicts { get; private set; } = [];
    public BrowserCapabilities Capabilities { get; private set; } = new(false, true, false);
    public string? FolderName { get; private set; }
    public string Theme { get; private set; } = "system";
    public string Status { get; private set; } = "Saved on this device";
    public string? Error { get; private set; }
    public string? CatalogNotice { get; private set; }
    public bool Busy { get; private set; }
    public bool Ready { get; private set; }
    public bool EditingBlocked { get; private set; }
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        browser = await js.InvokeAsync<IJSObjectReference>("import", "./app.js");
        using var request = new HttpRequestMessage(HttpMethod.Get, "data/catalog.json");
        // Cached service-worker responses must be buffered before catalog decoding.
        request.SetBrowserResponseStreamingEnabled(false);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Catalog = JsonSerializer.Deserialize<Catalog>(await response.Content.ReadAsStringAsync(), TrackerJson.Options)
            ?? throw new InvalidDataException("The catalog is empty.");
        Pokemon = Catalog.Pokemon.ToDictionary(p => p.Id);
        Capabilities = await browser.InvokeAsync<BrowserCapabilities>("capabilities");
        Theme = await browser.InvokeAsync<string>("getTheme");
        string saved = await browser.InvokeAsync<string>("loadLocal");
        Trackers = JsonSerializer.Deserialize<List<LocalTracker>>(saved, TrackerJson.Options)
            ?? throw new InvalidDataException("Local progress could not be read.");
        foreach (var tracker in Trackers) TrackerJson.Parse(TrackerJson.Serialize(tracker.State));
        if (Trackers.Any(t => t.State.CatalogVersion != Catalog.Version))
            CatalogNotice = "The catalog has changed. Your checks were preserved by Pokémon identity. New entries are unchecked; corrected entries may have moved to different boxes.";
        Ready = true;
        Status = Trackers.Any(t => t.Pending) ? "Saved on this device · folder changes pending" : "Saved on this device";
    }

    public DexDefinition Dex(LocalTracker tracker) => Catalog.Dexes.Single(d => d.Id == tracker.State.DexId);
    public IEnumerable<LocalTracker> VisibleTrackers => Trackers.Where(t => !t.Deleted);
    public int CheckedCount(LocalTracker tracker) => Catalog.Dexes.SingleOrDefault(d => d.Id == tracker.State.DexId)?.Entries.Count(e => tracker.State.Checked.Contains(e.PokemonId)) ?? 0;

    public Task CreateAsync(string name, string gameId, string dexId) => EditAsync(() =>
    {
        if (Catalog.Games.SingleOrDefault(g => g.Id == gameId)?.Group is not { } group || !Catalog.Dexes.Any(d => d.Id == dexId && d.Group == group)) throw new InvalidDataException("Unknown game or incompatible Pokédex.");
        Trackers.Add(new() { State = new() { Id = Guid.NewGuid(), Name = ValidName(name), GameId = gameId, DexId = dexId, CatalogVersion = Catalog.Version, LastEditedUtc = DateTimeOffset.UtcNow } });
    });
    public Task ToggleAsync(Guid id, string pokemonId, bool value) => EditAsync(() =>
    {
        int index = Index(id);
        LocalTracker tracker = Trackers[index];
        if (!Dex(tracker).Entries.Any(entry => entry.PokemonId == pokemonId)) throw new InvalidDataException("This Pokémon is not in this Pokédex.");
        Trackers[index] = tracker with { State = tracker.State.SetChecked(pokemonId, value, DateTimeOffset.UtcNow) with { CatalogVersion = Catalog.Version }, Pending = true };
    });
    public Task RenameAsync(Guid id, string name) => EditAsync(() => Update(id, state => state with { Name = ValidName(name) }));
    public Task ResetAsync(Guid id) => EditAsync(() => Update(id, state => state with { Checked = [] }));
    public Task DuplicateAsync(Guid id) => EditAsync(() =>
    {
        var original = Trackers[Index(id)].State;
        Trackers.Add(new() { State = original with { Id = Guid.NewGuid(), Name = original.Name + " copy", Checked = new(original.Checked), LastEditedUtc = DateTimeOffset.UtcNow } });
    });
    public Task DeleteAsync(Guid id) => EditAsync(() =>
    {
        int index = Index(id);
        if (Trackers[index].SyncedFiles.Count == 0) Trackers.RemoveAt(index);
        else Trackers[index] = Trackers[index] with { Deleted = true, Pending = true, State = Trackers[index].State with { LastEditedUtc = DateTimeOffset.UtcNow } };
    });

    public async Task ChooseFolderAsync()
    {
        try { var interop = browser ?? throw new InvalidOperationException("The application has not loaded."); FolderName = await interop.InvokeAsync<string>("chooseFolder"); provider = new FolderStorageProvider(interop); Error = null; }
        catch (JSException ex) { Error = Friendly(ex); }
        Changed?.Invoke();
    }
    public async Task ReconnectAsync()
    {
        try
        {
            var interop = browser ?? throw new InvalidOperationException("The application has not loaded.");
            FolderName = await interop.InvokeAsync<string?>("reconnectFolder");
            if (FolderName is null) Error = "No folder has been selected on this device yet.";
            else { provider = new FolderStorageProvider(interop); Error = null; }
        }
        catch (JSException ex) { Error = Friendly(ex); }
        Changed?.Invoke();
    }
    public async Task DisconnectAsync()
    {
        await browser!.InvokeVoidAsync("forgetFolder");
        provider = null; FolderName = null; Conflicts = []; Changed?.Invoke();
    }
    public async Task SyncAsync() => await SerializedAsync(async () =>
    {
        if (provider is null) { Error = "Connect your sync folder first."; return; }
        await browser!.InvokeVoidAsync("validateLocal");
        Conflicts = [];
        List<StorageFile> files = await provider.ListAsync();
        var groups = GroupFiles(files);
        foreach (var pair in groups.Where(pair => Trackers.All(t => t.State.Id != pair.Key)))
        {
            var parsed = ValidCandidates(pair.Key, pair.Value);
            if (parsed.Count > 0 && pair.Value.All(f => f.IntegrityValid) && pair.Value.Select(f => f.Hash).Distinct().Count() == 1)
                Trackers.Add(new() { State = parsed[0], SyncedFiles = pair.Value.Select(f => f.FileName).ToList(), Pending = false, LastSyncedUtc = DateTimeOffset.UtcNow });
            else Conflicts.Add(new(pair.Key, "This folder tracker has conflicting, unreadable, or unsupported catalog data. Files have been preserved.", pair.Value));
        }
        await PersistAsync();
        foreach (LocalTracker snapshot in Trackers.ToList())
        {
            if (Conflicts.Any(c => c.TrackerId == snapshot.State.Id)) continue;
            groups.TryGetValue(snapshot.State.Id, out List<StorageFile>? remote);
            remote ??= [];
            if (remote.Count != ValidCandidates(snapshot.State.Id, remote).Count)
            { Conflicts.Add(new(snapshot.State.Id, "A tracker file has invalid data or a mismatched identity. Automatic writes are paused.", remote)); continue; }
            var decision = TrackerSync.Decide(snapshot, remote.Select(file => file.Stored).ToList());
            try
            {
                switch (decision)
                {
                    case SyncDecision.Conflict conflict: Conflicts.Add(new(snapshot.State.Id, conflict.Reason, remote)); break;
                    case SyncDecision.Import import:
                        if (remote.Count > 1) await SaveToProviderAsync(new() { State = import.State }, remote);
                        else Accept(import.State, import.Files);
                        break;
                    case SyncDecision.Save:
                        await SaveToProviderAsync(snapshot, remote); break;
                    case SyncDecision.Unchanged when remote.Count > 1:
                        await SaveToProviderAsync(snapshot, remote); break;
                }
            }
            catch (JSException ex)
            {
                var latest = GroupFiles(await provider.ListAsync()).GetValueOrDefault(snapshot.State.Id) ?? [];
                Conflicts.Add(new(snapshot.State.Id, "Folder save interrupted: " + Friendly(ex), latest));
            }
            // A later tracker failure cannot erase an earlier tracker's successful baseline.
            await PersistAsync();
        }
        await PersistAsync();
        Status = Conflicts.Count > 0 ? $"{Conflicts.Count} tracker conflict(s) need your choice" : "Folder synchronized · other devices depend on your folder sync tool";
    });

    public async Task ResolveAsync(Guid id, string? fileName, bool useLocal) => await SerializedAsync(async () =>
    {
        if (provider is null) throw new InvalidOperationException("Reconnect the folder first.");
        await browser!.InvokeVoidAsync("validateLocal");
        TrackerConflict conflict = Conflicts.Single(c => c.TrackerId == id);
        var current = GroupFiles(await provider.ListAsync()).GetValueOrDefault(id) ?? [];
        if (!SameFiles(current, conflict.Candidates)) throw new InvalidDataException("The folder changed again. Synchronize again before making this choice.");
        if (useLocal)
        {
            var local = Trackers.Single(t => t.State.Id == id);
            await SaveToProviderAsync(local, current);
        }
        else if (fileName is not null)
        {
            var candidate = current.Single(f => f.FileName == fileName);
            var state = TrackerJson.Parse(candidate.Content);
            if (state.Id != id) throw new InvalidDataException("The selected file has a different tracker identity.");
            var local = new LocalTracker { State = state, Pending = true };
            await SaveToProviderAsync(local, current);
        }
        else
        {
            // An acknowledged missing remote file means accepting deletion here.
            RememberDeletion(Trackers.Single(t => t.State.Id == id));
        }
        Conflicts.RemoveAll(c => c.TrackerId == id);
        await PersistAsync();
        Status = "Conflict resolved · saved on this device and in the folder";
    });

    public async Task ExportAsync()
    {
        var backup = new TrackerBackup(1, DateTimeOffset.UtcNow, VisibleTrackers.Select(t => t.State).ToList());
        await browser!.InvokeVoidAsync("download", $"pokedex-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json", JsonSerializer.Serialize(backup, TrackerJson.Options));
    }
    public async Task ImportAsync(string content) => await EditAsync(() =>
    {
        var backup = JsonSerializer.Deserialize<TrackerBackup>(content, TrackerJson.Options) ?? throw new InvalidDataException("The backup is empty.");
        if (backup.FormatVersion != 1 || backup.Trackers is null) throw new InvalidDataException("This backup format is not supported.");
        var parsed = backup.Trackers.Select(state => TrackerJson.Parse(TrackerJson.Serialize(state))).ToList();
        if (parsed.Any(state => !Supported(state))) throw new InvalidDataException("The backup uses a game or Pokédex this catalog does not support.");
        // Imports create independent copies, so current local/folder progress cannot be overwritten.
        Trackers.AddRange(parsed.Select(state => new LocalTracker { State = state with { Id = Guid.NewGuid(), Name = state.Name + " imported", LastEditedUtc = DateTimeOffset.UtcNow } }));
        Status = $"Imported {parsed.Count} independent tracker(s)";
    });
    public async Task SetThemeAsync(string theme)
    {
        if (theme is not ("system" or "dark" or "light")) return;
        await browser!.InvokeVoidAsync("setTheme", theme); Theme = theme; Changed?.Invoke();
    }
    public ValueTask ShowDialogAsync(string id) => browser!.InvokeVoidAsync("openDialog", id);
    public ValueTask CloseDialogAsync(string id) => browser!.InvokeVoidAsync("closeDialog", id);
    public async Task RequestPersistentStorageAsync()
    {
        bool result = await browser!.InvokeAsync<bool>("persistStorage");
        Status = result ? "Browser granted persistent storage" : "Browser did not grant persistent storage. Export backups to keep a separate copy."; Changed?.Invoke();
    }
    public void DismissError() { Error = null; Changed?.Invoke(); }
    public void ReportError(string message) { Error = message; Changed?.Invoke(); }
    public TrackerState? ReadCandidate(TrackerConflict conflict, StorageFile file)
    {
        try { var state = TrackerJson.Parse(file.Content); return state.Id == conflict.TrackerId && Supported(state) ? state : null; }
        catch (Exception ex) when (ex is JsonException or InvalidDataException) { return null; }
    }
    private async Task SaveToProviderAsync(LocalTracker tracker, List<StorageFile> remote)
    {
        if (tracker.Deleted)
        {
            await provider!.CommitAsync(tracker.State.Id, remote, null, null);
            RememberDeletion(tracker);
            return;
        }
        string content = TrackerJson.Serialize(tracker.State);
        string filename = TrackerJson.FileName(tracker.State);
        var remaining = await provider!.CommitAsync(tracker.State.Id, remote, filename, content);
        Accept(tracker.State, remaining.Select(f => f.FileName).ToList());
    }
    private void Accept(TrackerState state, List<string> files)
    {
        var next = new LocalTracker { State = state, SyncedFiles = files, LastSyncedUtc = DateTimeOffset.UtcNow, Pending = false };
        int index = Trackers.FindIndex(t => t.State.Id == state.Id);
        if (index < 0) Trackers.Add(next); else Trackers[index] = next;
    }
    private void RememberDeletion(LocalTracker tracker)
    {
        int index = Index(tracker.State.Id);
        Trackers[index] = tracker with { State = tracker.State with { Checked = [] }, Deleted = true, Pending = false, SyncedFiles = [], LastSyncedUtc = DateTimeOffset.UtcNow };
    }
    private Dictionary<Guid, List<StorageFile>> GroupFiles(List<StorageFile> files)
    {
        Dictionary<Guid, List<StorageFile>> result = [];
        foreach (StorageFile file in files)
        {
            Guid id;
            try { id = Guid.TryParse(file.FilenameId, out Guid filenameId) ? filenameId : TrackerJson.Parse(file.Content).Id; }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            { if (!Guid.TryParse(file.FilenameId, out id)) { Error = $"Unreadable tracker file '{file.FileName}' was preserved."; continue; } }
            if (!result.TryGetValue(id, out var group)) result[id] = group = [];
            bool matches = file.FilenameId == id.ToString("N");
            group.Add(file with { IntegrityValid = file.IntegrityValid && matches });
        }
        return result;
    }
    private List<TrackerState> ValidCandidates(Guid id, List<StorageFile> files) => files.Select(file => ReadCandidate(new(id, "", files), file)).OfType<TrackerState>().ToList();
    private static bool SameFiles(List<StorageFile> left, List<StorageFile> right) => left.Select(f => f.FileName + ":" + f.Hash).Order().SequenceEqual(right.Select(f => f.FileName + ":" + f.Hash).Order());
    private bool Supported(TrackerState state) => Catalog.Games.SingleOrDefault(g => g.Id == state.GameId)?.Group is { } group && Catalog.Dexes.Any(d => d.Id == state.DexId && d.Group == group);
    private Task PersistAsync() => browser!.InvokeVoidAsync("saveLocal", JsonSerializer.Serialize(Trackers, TrackerJson.Options)).AsTask();
    private Task EditAsync(Action change) => SerializedAsync(async () =>
    {
        var previous = Trackers.ToList();
        try { change(); await PersistAsync(); }
        catch { Trackers = previous; throw; }
        Status = "Saved on this device · folder changes pending";
    });
    private async Task SerializedAsync(Func<Task> action)
    {
        await operations.WaitAsync(); Busy = true; Error = null; Changed?.Invoke();
        try
        {
            if (EditingBlocked) throw new InvalidOperationException("Another tab changed your trackers. Reload this tab before editing or synchronizing.");
            await action();
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidDataException or InvalidOperationException)
        {
            Error = ex is JSException j ? Friendly(j) : ex.Message;
            if (Error.Contains("Another tab", StringComparison.Ordinal)) EditingBlocked = true;
        }
        finally { Busy = false; operations.Release(); Changed?.Invoke(); }
    }
    private int Index(Guid id) => Trackers.FindIndex(t => t.State.Id == id) is var index && index >= 0 ? index : throw new InvalidDataException("Tracker not found.");
    private void Update(Guid id, Func<TrackerState, TrackerState> update)
    {
        int index = Index(id); var tracker = Trackers[index];
        Trackers[index] = tracker with { State = update(tracker.State) with { LastEditedUtc = DateTimeOffset.UtcNow, CatalogVersion = Catalog.Version }, Pending = true };
    }
    private static string ValidName(string name) => string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120 ? throw new InvalidDataException("Use a tracker name between 1 and 120 characters.") : name.Trim();
    private static string Friendly(JSException error) => error.Message.Split('\n')[0];
    public async ValueTask DisposeAsync() { if (browser is not null) await browser.DisposeAsync(); operations.Dispose(); }
}

public sealed record TrackerBackup(int FormatVersion, DateTimeOffset ExportedUtc, List<TrackerState> Trackers);
