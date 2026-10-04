using System.Text.Json;
using PokedexTracker.Core;

string root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
Catalog catalog = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(Path.Combine(root, "pokedex_tracker/wwwroot/data/catalog.json")), TrackerJson.Options)!;
int checks = 0;
void Check(bool condition, string description) { if (!condition) throw new InvalidOperationException(description); checks++; }
DexDefinition Dex(string id) => catalog.Dexes.Single(d => d.Id == id);
Check(catalog.Games.Count == 13, "All 12 supported editions and HOME exist.");
Check(catalog.Pokemon.Select(p => p.Id).Distinct().Count() == catalog.Pokemon.Count, "Pokémon identities are unique.");
Check(catalog.Dexes.Select(d => d.Id).Distinct().Count() == catalog.Dexes.Count, "Dex identities are unique.");
Check(Dex("national").Entries.Count(e => !e.Extra) == 1025, "National contains 1,025 species.");
Check(Dex("national").Entries.Count == 1083, "National contains all 58 additional regional identities.");
var expectedCounts = new Dictionary<string, int> { ["letsgo-kanto"] = 153, ["galar"] = 400, ["isle-of-armor"] = 211, ["crown-tundra"] = 210, ["sinnoh"] = 151, ["bdsp-national"] = 493, ["hisui"] = 242, ["paldea"] = 400, ["kitakami"] = 200, ["blueberry"] = 243, ["lumiose"] = 232, ["hyperspace"] = 132, ["kanto"] = 151, ["frlg-national"] = 386 };
foreach (var pair in expectedCounts) Check(Dex(pair.Key).Entries.Count(e => !e.Extra) == pair.Value, pair.Key + " numbered membership");
foreach (var dex in catalog.Dexes)
{
    Check(dex.Columns == 6 && dex.Rows == 5, dex.Id + " has 30 slots in the confirmed layout.");
    Check(dex.Entries.Select(e => e.PokemonId).Distinct().Count() == dex.Entries.Count, dex.Id + " does not duplicate an identity.");
    Check(!dex.Entries.SkipWhile(e => !e.Extra).Any(e => !e.Extra), dex.Id + " extras follow all numbered entries.");
    foreach (var entry in dex.Entries)
        Check(catalog.Pokemon.Any(p => p.Id == entry.PokemonId), dex.Id + " references a known Pokémon.");
}
Check(Dex("paldea").Entries.Single(e => e.Number == 53).PokemonId == "wooper-paldea", "Paldea Wooper uses its Paldean slot.");
Check(Dex("kitakami").Entries.Any(e => e.PokemonId == "basculin-white-striped" && !e.Extra), "Kitakami uses White-striped Basculin.");
Check(Dex("kitakami").Entries.Any(e => e.PokemonId == "growlithe-hisui" && e.Extra), "Kitakami includes the Hisuian reward.");
Check(Dex("kitakami").Entries.Any(e => e.PokemonId == "arcanine-hisui" && e.Extra), "Kitakami includes its regional evolution.");
Check(Dex("blueberry").Entries.Any(e => e.PokemonId == "exeggutor" && !e.Extra) && Dex("blueberry").Entries.Any(e => e.PokemonId == "exeggutor-alola" && e.Extra), "Blueberry preserves Jeff's Exeggutor layout.");
Check(Dex("blueberry").Entries.Any(e => e.PokemonId == "slowpoke-galar" && !e.Extra), "Blueberry Slowpoke is Galarian.");
Check(Dex("blueberry").Entries.Any(e => e.PokemonId == "tauros" && !e.Extra), "Blueberry Tauros is Kantonian.");
Check(Dex("hisui").Entries.Any(e => e.PokemonId == "vulpix-alola" && e.Extra) && Dex("hisui").Entries.All(e => e.PokemonId != "growlithe"), "Arceus forms respect game compatibility.");
var complete = Dex("sv-complete").Entries;
Check(complete.Take(400).SequenceEqual(Dex("paldea").Entries.Where(e => !e.Extra)), "Combined starts with all 400 numbered Paldea entries.");
Check(complete.Single(e => e.PokemonId == "wooper").Region == "Kitakami" && !complete.Single(e => e.PokemonId == "wooper").Extra, "Kitakami numbered Wooper overrides its earlier extra position.");
Check(complete.Count(e => e.PokemonId is "basculin" or "basculin-white-striped") == 2, "Combined Basculin identities remain distinct.");
foreach (var p in catalog.Pokemon)
{
    Check(File.Exists(Path.Combine(root, "pokedex_tracker/wwwroot/sprites", p.SpriteId + ".png")), "Sprite exists for " + p.Id);
    Check(p.Sources.Values.All(source => source.Url.StartsWith("https://www.serebii.net/", StringComparison.Ordinal)), "Sources link to Serebii.");
}
var pikachu = catalog.Pokemon.Single(p => p.Id == "pikachu");
Check(pikachu.Sources["letsgo-pikachu"].Areas.Contains("Viridian Forest"), "Let's Go route extraction returns Viridian Forest.");
Check(pikachu.Sources["legends-za"].Areas.Contains("Wild Zone 3"), "Z-A route extraction returns Wild Zone 3.");
Check(catalog.Pokemon.Single(p => p.Id == "growlithe-hisui").Sources["scarlet"].Method == "Perrin's reward", "Regional gift overrides do not show ordinary encounters.");
Check(catalog.Pokemon.Single(p => p.Id == "basculin-white-striped").Sources["scarlet"].Areas.SequenceEqual(["Timeless Woods"]), "White-striped Basculin does not inherit ordinary Paldea encounters.");
Check(catalog.Pokemon.Single(p => p.Id == "vulpix-alola").Sources["scarlet"].Areas.Contains("Polar Biome") && !catalog.Pokemon.Single(p => p.Id == "vulpix-alola").Sources["scarlet"].Areas.Contains("Kitakami Road"), "Alolan Vulpix uses its own encounter region.");

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
Console.WriteLine($"PASS: {checks} catalog and tracker behavior checks.");
