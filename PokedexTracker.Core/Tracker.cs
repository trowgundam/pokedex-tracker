using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PokedexTracker.Core;

public sealed record TrackerState
{
    public int FormatVersion { get; init; } = 1;
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string GameId { get; init; }
    public required string DexId { get; init; }
    public required string CatalogVersion { get; init; }
    public required DateTimeOffset LastEditedUtc { get; init; }
    public HashSet<string> Checked { get; init; } = [];

    public TrackerState SetChecked(string pokemonId, bool value, DateTimeOffset time)
    {
        HashSet<string> next = new(Checked);
        if (value) next.Add(pokemonId); else next.Remove(pokemonId);
        return this with { Checked = next, LastEditedUtc = time };
    }
}

public sealed record LocalTracker
{
    public required TrackerState State { get; init; }
    public List<string> SyncedFiles { get; init; } = [];
    public DateTimeOffset? LastSyncedUtc { get; init; }
    public bool Pending { get; init; } = true;
    public bool Deleted { get; init; }
}

public sealed record StoredTracker(string FileName, string Content, string Hash, bool IntegrityValid);

public static class TrackerJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false
    };

    public static string Serialize(TrackerState state) => JsonSerializer.Serialize(new
    {
        state.FormatVersion, state.Id, state.Name, state.GameId, state.DexId,
        state.CatalogVersion, state.LastEditedUtc,
        Checked = state.Checked.Order(StringComparer.Ordinal).ToArray()
    }, Options);

    public static TrackerState Parse(string content)
    {
        TrackerState state = JsonSerializer.Deserialize<TrackerState>(content, Options)
            ?? throw new InvalidDataException("The tracker file is empty.");
        if (state.FormatVersion != 1 || state.Id == Guid.Empty ||
            string.IsNullOrWhiteSpace(state.Name) || string.IsNullOrWhiteSpace(state.GameId) ||
            string.IsNullOrWhiteSpace(state.DexId) || string.IsNullOrWhiteSpace(state.CatalogVersion) ||
            state.LastEditedUtc == default || state.Checked is null ||
            state.Checked.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("The file is not a supported tracker state.");
        }
        return state;
    }

    public static string Hash(string content) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    public static string FileName(TrackerState state) => $"tracker-{state.Id:N}-{Hash(Serialize(state))}.json";
}

public abstract record SyncDecision
{
    public sealed record Unchanged : SyncDecision;
    public sealed record Import(TrackerState State, List<string> Files) : SyncDecision;
    public sealed record Save(List<string> ExpectedFiles) : SyncDecision;
    public sealed record Conflict(List<StoredTracker> Versions, string Reason) : SyncDecision;
}

public static class TrackerSync
{
    public static SyncDecision Decide(LocalTracker local, IReadOnlyList<StoredTracker> remote)
    {
        List<string> files = remote.Select(file => file.FileName).Order(StringComparer.Ordinal).ToList();
        if (remote.Any(file => !file.IntegrityValid))
            return new SyncDecision.Conflict(remote.ToList(), "A file's contents do not match its filename hash.");
        if (remote.Select(file => file.Hash).Distinct().Count() > 1)
            return new SyncDecision.Conflict(remote.ToList(), "The folder contains different states for this tracker.");
        if (local.Deleted && !local.Pending)
            return remote.Count == 0 ? new SyncDecision.Unchanged() : new SyncDecision.Conflict(remote.ToList(), "A folder version reappeared after this tracker was deleted.");
        bool sameBaseline = files.SequenceEqual(local.SyncedFiles.Order(StringComparer.Ordinal));
        if (local.Pending || local.Deleted)
        {
            if (sameBaseline) return new SyncDecision.Save(files);
            if (!local.Deleted && remote.Count > 0 && remote.All(file => file.Hash == TrackerJson.Hash(TrackerJson.Serialize(local.State))))
                return new SyncDecision.Import(local.State, files);
            return new SyncDecision.Conflict(remote.ToList(), "The folder changed since this device last synchronized.");
        }
        if (remote.Count == 0)
            return new SyncDecision.Conflict([], "The previously synchronized tracker is missing from the folder.");
        if (sameBaseline) return new SyncDecision.Unchanged();
        return new SyncDecision.Import(TrackerJson.Parse(remote[0].Content), files);
    }
}
