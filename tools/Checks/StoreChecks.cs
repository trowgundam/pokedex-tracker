using System.Net;
using System.Text.Json;

using Microsoft.JSInterop;

using PokedexTracker.Core;
using PokedexTracker.Services;

internal static class StoreChecks
{
    public static async Task<int> Run(Catalog catalog)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
            checks++;
        }
        var browser = new CheckBrowser();
        await using var store = new TrackerStore(new HttpClient(new CatalogResponse(catalog)) { BaseAddress = new("https://checks.invalid/") }, browser);
        await store.InitializeAsync();
        await store.CreateAsync(new string('A', 120), "scarlet", "paldea");
        Guid id = store.VisibleTrackers.Single().State.Id;
        await store.ToggleAsync(id, "sprigatito", true);
        Check(store.Trackers.Single().State.Checked.SetEquals(["sprigatito"]) && browser.Saved.Single().State.Checked.SetEquals(["sprigatito"]), "A store edit persists the selected check.");
        browser.FailSave = true;
        await store.ToggleAsync(id, "fuecoco", true);
        Check(store.Trackers.Single().State.Checked.SetEquals(["sprigatito"]) && browser.Saved.Single().State.Checked.SetEquals(["sprigatito"]), "A failed save reverts only the failed edit.");
        Check(store.Error == "Could not save on this device. Your previously saved progress is unchanged. The last edit was reverted.", "Save failure explains the rollback.");
        browser.FailSave = false;
        await store.DuplicateAsync(id);
        var copy = store.VisibleTrackers.Last();
        Check(copy.State.Name == new string('A', 115) + " copy" && copy.State.Id != id, "A duplicate preserves its suffix within the name limit and has a new identity.");
        await store.RenameAsync(copy.State.Id, copy.State.Name);
        Check(store.Error is null && browser.Saved.Last().State.Name == new string('A', 115) + " copy", "The generated name can be saved unchanged through rename.");
        await store.ToggleAsync(copy.State.Id, "sprigatito", false);
        Check(store.Trackers.Single(t => t.State.Id == id).State.Checked.SetEquals(["sprigatito"]) && store.Trackers.Single(t => t.State.Id == copy.State.Id).State.Checked.Count == 0, "Duplicate edits remain independent.");
        var backup = new TrackerBackup(1, DateTimeOffset.UtcNow, [store.Trackers[0].State]);
        await store.ImportAsync(JsonSerializer.Serialize(backup, TrackerJson.Options));
        var imported = store.VisibleTrackers.Last();
        Check(imported.State.Name == new string('A', 111) + " imported" && imported.State.Id != id && imported.State.Checked.SetEquals(["sprigatito"]), "Imported names fit the limit without losing progress or identity independence.");
        await store.RenameAsync(imported.State.Id, imported.State.Name);
        Check(store.Error is null, "Imported names can be resubmitted through rename.");
        int before = store.Trackers.Count;
        await store.ImportAsync(JsonSerializer.Serialize(new TrackerBackup(1, DateTimeOffset.UtcNow, [backup.Trackers[0], backup.Trackers[0] with { GameId = "unknown" }]), TrackerJson.Options));
        Check(store.Trackers.Count == before && browser.Saved.Count == before && store.Error == "The backup uses a game or Pokédex this catalog does not support.", "An invalid batch import leaves all existing progress unchanged.");
        await store.SetThemeAsync("normal");
        await store.SetColorSchemeAsync("dark");
        Check(store.Theme == "normal" && store.ColorScheme == "dark" && store.Status == "Appearance changed for this session. The browser could not save the preference.", "Preference-save failures retain the session appearance with an explanation.");
        browser.Stale = true;
        await store.ToggleAsync(id, "quaxly", true);
        Check(store.EditingBlocked && store.Trackers.Single(t => t.State.Id == id).State.Checked.SetEquals(["sprigatito"]), "A stale tab cannot overwrite local progress.");
        return checks;
    }

    private sealed class CatalogResponse(Catalog catalog) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(catalog, TrackerJson.Options)) });
    }
    private sealed class CheckBrowser : IJSRuntime, IJSObjectReference
    {
        private string content = "[]";
        public bool FailSave { get; set; }
        public bool Stale { get; set; }
        public List<LocalTracker> Saved => JsonSerializer.Deserialize<List<LocalTracker>>(content, TrackerJson.Options)!;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, CancellationToken.None, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "saveLocal")
            {
                if (Stale) throw new JSException("Another tab changed your trackers. Reload this tab before editing to protect those changes.");
                if (FailSave) throw new JSException("Could not save on this device. Your previously saved progress is unchanged.");
                content = (string)args![0]!;
            }
            object? result = identifier switch
            {
                "import" => this,
                "capabilities" => new BrowserCapabilities(false, true, false),
                "getColorScheme" => "system",
                "getThemeChoice" => "catppuccin",
                "getThemeAccent" => "blue",
                "loadLocal" => content,
                "setThemeChoice" or "setThemeAccent" or "setColorScheme" => false,
                _ => null
            };
            return ValueTask.FromResult(result is null ? default(T)! : (T)result);
        }
    }
}