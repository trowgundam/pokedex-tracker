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
Check(Dex("paldea").Entries.Where(e => e.Extra).Select(e => e.PokemonId).SequenceEqual(["meowth-galar", "perrserker", "wooper", "quagsire"]), "Paldea extras are exactly the obtainable trades, gift, and their evolutions.");
Check(Dex("kitakami").Entries.Where(e => e.Extra).Select(e => e.PokemonId).SequenceEqual(["growlithe-hisui", "arcanine-hisui", "tauros"]), "Kitakami extras are exactly Perrin's reward, its evolution, and locally bred Tauros.");
Check(Dex("blueberry").Entries.Where(e => e.Extra).Select(e => e.PokemonId).SequenceEqual(["exeggutor-alola", "meowth-alola", "persian-alola"]), "Blueberry extras are exactly its obtainable Alolan Exeggutor and traded Meowth line.");
Check(Dex("lumiose").Entries.Where(e => e.Extra).Select(e => e.PokemonId).SequenceEqual(["raichu-alola", "slowpoke-galar", "slowbro-galar", "slowking-galar", "stunfisk-galar"]), "Lumiose excludes regional forms introduced only in Mega Dimension.");
Check(Dex("galar").Entries.Where(e => e.Extra).Select(e => e.PokemonId).SequenceEqual(["meowth", "slowpoke-galar", "mr-mime", "yamask"]), "Galar extras require only base-game trades and the Wedgehurst encounter.");
Check(Dex("isle-of-armor").Entries.Single(e => e.Number == 3 && !e.Extra).PokemonId == "slowking", "Isle of Armor uses its obtainable ordinary Slowking, without requiring a Crown Tundra evolution item.");
Check(Dex("isle-of-armor").Entries.Any(e => e.PokemonId == "persian-alola" && e.Extra) && Dex("isle-of-armor").Entries.Any(e => e.PokemonId == "sirfetchd" && e.Extra), "Isle of Armor includes locally obtainable regional families even without a numbered species slot.");
Check(Dex("isle-of-armor").Entries.All(e => e.PokemonId is not ("slowking-galar" or "moltres-galar")), "Isle of Armor excludes Crown quest and item dependencies despite their encounter locations.");
Check(Dex("crown-tundra").Entries.Any(e => e.PokemonId == "raichu-alola" && e.Extra) && Dex("crown-tundra").Entries.Any(e => e.PokemonId == "marowak-alola" && e.Extra), "Crown Tundra includes native Max Lair regional encounters.");
Check(Dex("crown-tundra").Entries.All(e => e.PokemonId is not ("ponyta" or "rapidash" or "darumaka" or "darmanitan")), "Crown Tundra excludes ordinary counterparts that require Isle of Armor.");
Check(Dex("isle-of-armor").Entries.Count(e => e.Extra) == 47 && Dex("crown-tundra").Entries.Count(e => e.Extra) == 41, "DLC extras include all audited native regional families.");
Check(Dex("crown-tundra").Entries.All(e => e.PokemonId is not ("exeggutor-alola" or "slowbro-galar")), "Crown Tundra does not borrow Alolan Exeggutor or the Galarica Cuff from Isle of Armor.");
Check(Dex("hyperspace").Entries.Count(e => e.Extra) == 16 && Dex("hyperspace").Entries.Any(e => e.PokemonId == "goodra-hisui" && e.Extra), "Mega Dimension retains its native regional encounters.");
Check(Dex("blueberry").Entries.Any(e => e.PokemonId == "walking-wake" && !e.Extra) && Dex("blueberry").Entries.Any(e => e.PokemonId == "iron-leaves" && !e.Extra) && Dex("isle-of-armor").Entries.Any(e => e.PokemonId == "zarude" && !e.Extra), "Numbered event entries remain trackable.");
Check(new[] { "manaphy", "phione", "darkrai", "shaymin" }.All(id => Dex("bdsp-national").Entries.Any(e => e.PokemonId == id && !e.Extra)), "BDSP retains numbered historical gifts and their offspring.");
Check(Dex("letsgo-kanto").Entries.All(e => e.PokemonId is not ("meltan" or "melmetal")), "Let's Go excludes the two GO-transfer-only species.");
Check(Dex("letsgo-kanto").Entries.Any(e => e.PokemonId == "mew"), "Let's Go retains its direct Poké Ball Plus Mystery Gift.");
Check(Dex("bdsp-national").Entries.All(e => e.PokemonId is not ("celebi" or "deoxys")), "BDSP excludes HOME-only Celebi and Deoxys.");
Check(Dex("kanto").Entries.All(e => e.PokemonId != "mew") && Dex("frlg-national").Entries.All(e => e.PokemonId != "mew"), "Switch FRLG excludes Mew without a native distribution.");
Check(Dex("frlg-national").Entries.Count == 215, "Switch FRLG National contains the 215 species obtainable across its two editions.");
Check(Dex("frlg-national").Entries.Where(e => e.Number > 251).Select(e => e.PokemonId).SequenceEqual(["azurill", "wynaut", "deoxys"]), "Switch FRLG excludes Hoenn species that require a different generation-three title.");
Check(Dex("frlg-national").Entries.Any(e => e.PokemonId == "lugia") && Dex("frlg-national").Entries.Any(e => e.PokemonId == "ho-oh"), "Switch FRLG retains the native ticket encounters.");
Check(Dex("frlg-national").Entries.All(e => e.PokemonId is not ("mareep" or "aipom" or "pineco" or "shuckle" or "teddiursa" or "houndour" or "stantler" or "smeargle")), "Unreleased Altering Cave encounters do not establish native availability.");
var expectedCounts = new Dictionary<string, int> { ["letsgo-kanto"] = 151, ["galar"] = 400, ["isle-of-armor"] = 211, ["crown-tundra"] = 210, ["sinnoh"] = 151, ["bdsp-national"] = 491, ["hisui"] = 242, ["paldea"] = 400, ["kitakami"] = 200, ["blueberry"] = 243, ["lumiose"] = 232, ["hyperspace"] = 132, ["kanto"] = 150, ["frlg-national"] = 215 };
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
Check(complete.Single(e => e.PokemonId == "tauros").Region == "Blueberry" && !complete.Single(e => e.PokemonId == "tauros").Extra, "Kitakami bred Tauros folds into its numbered Blueberry position.");
Check(complete.Where(e => e.Extra).Select(e => e.PokemonId).SequenceEqual(["meowth-galar", "perrserker", "growlithe-hisui", "arcanine-hisui", "exeggutor-alola", "meowth-alola", "persian-alola"]), "Combined SV extras exclude every transfer-only form and deduplicate numbered DLC entries.");
foreach (var p in catalog.Pokemon)
{
    Check(File.Exists(Path.Combine(root, "pokedex_tracker/wwwroot/sprites", p.SpriteId + ".png")), "Sprite exists for " + p.Id);
    Check(p.Sources.Values.SelectMany(sources => sources.Values).All(source => source.Url.StartsWith("https://www.serebii.net/", StringComparison.Ordinal)), "Sources link to Serebii.");
    Check(p.Sources.Values.SelectMany(sources => sources.Values).All(source => !source.Method.Contains("transfer", StringComparison.OrdinalIgnoreCase)), "Native checklist sources never advertise transfer for " + p.Id);
}
var pikachu = catalog.Pokemon.Single(p => p.Id == "pikachu");
Check(pikachu.SourceFor("letsgo-pikachu", Dex("national"))!.Areas.Contains("Viridian Forest"), "Let's Go route extraction returns Viridian Forest.");
Check(pikachu.SourceFor("legends-za", Dex("national"))!.Areas.Contains("Wild Zone 3"), "Z-A route extraction returns Wild Zone 3.");
foreach (string id in new[] { "tauros-paldea-blaze-breed", "tauros-paldea-aqua-breed" })
{
    var breed = catalog.Pokemon.Single(p => p.Id == id);
    Check(breed.Sources.Keys.Order().SequenceEqual(["scarlet", "violet"]), "National Tauros breeds have sources in both paired editions.");
    Check(breed.Sources.Values.SelectMany(sources => sources.Values).All(source => source.Url == "https://www.serebii.net/pokedex-sv/tauros/"), "Tauros breeds link to the species page rather than a nonexistent form URL.");
}
foreach (string game in new[] { "sword", "shield" })
    Check(new[] { "mewtwo", "mew", "keldeo", "treecko", "cosmog", "naganadel", "regigigas" }.All(id => catalog.Pokemon.Single(p => p.Id == id).Sources.ContainsKey(game)), "National sources include native catches, gifts, breeding, and evolutions outside the Sword/Shield numbered lists.");
foreach (string game in new[] { "sword", "shield" })
{
    var source = catalog.Pokemon.Single(p => p.Id == "mewtwo").SourceFor(game, Dex("national"))!;
    Check(source.Areas.SequenceEqual(["Max Lair"]) && source.Method == "Dynamax Adventures", "Mewtwo has its permanent Max Lair encounter rather than a historical event.");
}
Check(!catalog.Pokemon.Single(p => p.Id == "mew").Sources.ContainsKey("scarlet") && !catalog.Pokemon.Single(p => p.Id == "mew").Sources.ContainsKey("violet"), "National excludes expired SV Mew distribution outside its numbered Dex.");
foreach (string game in new[] { "scarlet", "violet" })
    Check(new[] { "rayquaza", "kyogre", "kubfu", "urshifu" }.All(id => catalog.Pokemon.Single(p => p.Id == id).Sources.ContainsKey(game)), "National sources include Snacksworth encounters and native evolution outside the SV numbered lists.");
Check(catalog.Pokemon.Single(p => p.Id == "growlithe-hisui").SourceFor("scarlet", Dex("national"))!.Method == "Perrin's reward", "Regional gift overrides do not show ordinary encounters.");
Check(catalog.Pokemon.Single(p => p.Id == "basculin-white-striped").SourceFor("scarlet", Dex("national"))!.Areas.SequenceEqual(["Timeless Woods"]), "White-striped Basculin does not inherit ordinary Paldea encounters.");
Check(catalog.Pokemon.Single(p => p.Id == "vulpix-alola").SourceFor("scarlet", Dex("national"))!.Areas.Contains("Polar Biome") && !catalog.Pokemon.Single(p => p.Id == "vulpix-alola").SourceFor("scarlet", Dex("national"))!.Areas.Contains("Kitakami Road"), "Alolan Vulpix uses its own encounter region.");
Check(catalog.Pokemon.Single(p => p.Id == "meowth-alola").SourceFor("scarlet", Dex("national"))!.Areas.SequenceEqual(["League Club Room"]), "Blueberry Alolan Meowth names its in-game trade location.");
Check(catalog.Pokemon.Single(p => p.Id == "tauros").SourceFor("scarlet", Dex("national"))!.Method.Contains("Breed Paldean Tauros in Kitakami"), "Kantonian Tauros explains its native Kitakami breeding source.");

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
checks += AcquisitionChecks.Run(catalog);
checks += EvolutionChecks.Run(catalog);
checks += EvolutionCoverageChecks.Run(catalog, root);
checks += await StoreChecks.Run(catalog);
Console.WriteLine($"PASS: {checks} catalog and tracker behavior checks.");
