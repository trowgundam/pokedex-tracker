using PokedexTracker.Core;

internal static class TrackerChecks
{
    public static async Task<int> Run(Catalog catalog)
    {
        int checks = 0;
        void Check(bool condition, string description) { if (!condition) throw new InvalidOperationException(description); checks++; }
        DexDefinition Dex(string id) => catalog.Dexes.Single(d => d.Id == id);
        DateTimeOffset now = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
        TrackerState original = new() { Id = Guid.NewGuid(), Name = "Living dex", GameId = "scarlet", DexId = "paldea", CatalogVersion = catalog.Version, LastEditedUtc = now };
        TrackerState edited = original.SetChecked("sprigatito", true, now.AddMinutes(1));
        Check(original.Checked.Count == 0 && edited.Checked.SetEquals(["sprigatito"]), "Changing a tracker cannot mutate a shared checklist.");
        var roundTrip = TrackerJson.Parse(TrackerJson.Serialize(edited));
        Check(roundTrip.Id == original.Id && roundTrip.Name == "Living dex" && roundTrip.GameId == "scarlet" && roundTrip.DexId == "paldea" && roundTrip.LastEditedUtc == now.AddMinutes(1) && roundTrip.Checked.SetEquals(["sprigatito"]), "State metadata and checks round trip.");
        Check(TrackerJson.Serialize(edited with { Checked = ["wooper", "sprigatito"] }) == TrackerJson.Serialize(edited with { Checked = ["sprigatito", "wooper"] }), "Checked order does not alter the content hash.");
        StoredTracker Stored(TrackerState state, string? name = null, bool integrity = true) { string content = TrackerJson.Serialize(state); return new(name ?? TrackerJson.FileName(state), content, TrackerJson.Hash(content), integrity); }
        var a = Stored(original); var b = Stored(edited);
        var local = new LocalTracker { State = original, Pending = false, SyncedFiles = [a.FileName], LastSyncedUtc = now };
        Check(TrackerSync.Decide(new() { State = original }, []) is SyncDecision.Save, "A new tracker is saved into an empty folder.");
        Check(TrackerSync.Decide(local, [a]) is SyncDecision.Unchanged, "An unchanged baseline causes no write.");
        Check(TrackerSync.Decide(local with { State = edited, Pending = true }, [a]) is SyncDecision.Save, "Local edits with an unchanged remote are saved.");
        Check(TrackerSync.Decide(local, [b]) is SyncDecision.Import { State.Checked.Count: 1 }, "Remote-only edits are imported.");
        Check(TrackerSync.Decide(local with { State = edited.SetChecked("quaxly", true, now.AddMinutes(2)), Pending = true }, [b]) is SyncDecision.Conflict, "Offline edits conflict with changes since the baseline.");
        Check(TrackerSync.Decide(local, [a, b]) is SyncDecision.Conflict, "Concurrent hash versions require acknowledgement.");
        Check(TrackerSync.Decide(local, [a with { IntegrityValid = false }]) is SyncDecision.Conflict, "A filename/content mismatch blocks automatic writes.");
        Check(TrackerSync.Decide(local, []) is SyncDecision.Conflict, "Another device must accept remote deletion explicitly.");
        Check(TrackerSync.Decide(local with { State = edited, Pending = true }, [b]) is SyncDecision.Import, "A retried identical save can converge without a conflict.");
        Check(TrackerSync.Decide(local with { Deleted = true, Pending = true }, [b]) is SyncDecision.Conflict, "Deletion cannot discard a remote edit without acknowledgement.");
        PokemonVariant SourceFixture(string id, string form, Dictionary<string, AcquisitionSource> sources) => new()
        { Id = id, Name = "Meowth", Form = form, NationalNumber = 52, SpriteId = 52, Sources = sources.ToDictionary(pair => pair.Key, pair => new Dictionary<string, AcquisitionSource> { ["paldea"] = pair.Value }) };
        var ordinary = SourceFixture("meowth", "", new()
        {
            ["scarlet"] = new(["Forest", "Route 1", "Route 1"], "Encounter", "https://www.serebii.net/"),
            ["leafgreen"] = new(["Route 1"], "Encounter", "https://www.serebii.net/")
        });
        var regional = SourceFixture("meowth-galar", "Galarian", new()
        {
            ["scarlet"] = new(["Forest"], "Gift", "https://www.serebii.net/"),
            ["violet"] = new(["Other location"], "Gift", "https://www.serebii.net/")
        });
        var unlocated = SourceFixture("perrserker", "", new()
        { ["scarlet"] = new([], "Evolution", "https://www.serebii.net/") });
        var ranking = AcquisitionRanking.Build([ordinary, regional, ordinary, unlocated], "scarlet", Dex("paldea"));
        Check(ranking.Locations.Select(location => (location.GameId, location.Area, location.Pokemon.Count)).SequenceEqual([("scarlet", "Forest", 2), ("scarlet", "Route 1", 1)]), "Locations rank by distinct outstanding variants in the selected edition, without duplicate areas or entries.");
        Check(ranking.Locations[0].Pokemon.Select(p => p.Id).SequenceEqual(["meowth", "meowth-galar"]), "Regional variants of the same species remain separate ranking entries.");
        Check(ranking.WithoutLocation.Select(p => p.Id).SequenceEqual(["perrserker"]), "Evolution-only entries without locations remain visible separately.");
        var afterCheck = AcquisitionRanking.Build([regional, unlocated], "scarlet", Dex("paldea"));
        Check(afterCheck.Locations.Select(location => (location.Area, location.Pokemon.Count)).SequenceEqual([("Forest", 1)]), "Checking a Pokémon decreases every associated location and removes empty locations.");
        var nationalRanking = AcquisitionRanking.Build([ordinary, regional, unlocated], "home", Dex("national"));
        Check(nationalRanking.Locations.Select(location => (location.GameId, location.Pokemon.Count)).SequenceEqual([("scarlet", 3), ("leafgreen", 1), ("violet", 1)]) && nationalRanking.Locations.All(location => location.Area is null), "Cross-game National sources rank games only and include evolution-only entries.");
        Check(nationalRanking.WithoutLocation.Count == 0, "National game availability does not require an encounter location.");
        Check(AcquisitionRanking.Build([regional], "leafgreen", Dex("paldea")).WithoutLocation.Select(p => p.Id).SequenceEqual(["meowth-galar"]), "A location in another edition does not count for this edition.");
        Check(AcquisitionRanking.Build([], "scarlet", Dex("paldea")) is { Locations.Count: 0, WithoutLocation.Count: 0 }, "A completed tracker has no outstanding sources.");
        checks += await StoreChecks.Run(catalog);
        return checks;
    }
}
