using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using PokedexTracker.Core;
using PokedexTracker.CatalogGenerator;

string root = Path.GetFullPath(args.FirstOrDefault(arg => !arg.StartsWith("--")) ?? ".");
string cache = Path.Combine(root, "artifacts/catalog-cache");
string output = Path.Combine(root, "pokedex_tracker/wwwroot");
Directory.CreateDirectory(cache);
Directory.CreateDirectory(Path.Combine(output, "data"));
Directory.CreateDirectory(Path.Combine(output, "sprites"));
using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(45) };
client.DefaultRequestHeaders.UserAgent.ParseAdd("PokedexTrackerCatalog/1.0");
const string dataCommit = "bc92d3b6029ef1abe9e7ad424c400b338f3c11fe";
const string spriteCommit = "a3a1432e688ea028f12c51371d5253037cb9f17b";
string dataCache = Path.Combine(cache, "pokeapi", dataCommit);
string spriteCache = Path.Combine(cache, "sprites", spriteCommit);
Directory.CreateDirectory(dataCache);
Directory.CreateDirectory(spriteCache);
bool evolutionMode = args.Any(arg => arg is "--evolutions-only" or "--audit-evolutions" or "--verify-evolutions");
string[] tables = ["pokemon", "pokemon_species", "pokemon_forms", "pokemon_evolution", "item_names", "move_names", "type_names"];
if (!evolutionMode) tables = [..tables, "pokemon_species_names", "pokemon_dex_numbers"];
foreach (string name in tables)
    await Download($"https://raw.githubusercontent.com/PokeAPI/pokeapi/{dataCommit}/data/v2/csv/{name}.csv", Path.Combine(dataCache, name + ".csv"));
if (evolutionMode)
{
    string catalogPath = Path.Combine(output, "data/catalog.json");
    Catalog existing = JsonSerializer.Deserialize<Catalog>(await File.ReadAllTextAsync(catalogPath), TrackerJson.Options)!;
    var evolutionBuild = await BuildEvolutions(existing);
    if (args.Contains("--audit-evolutions"))
    {
        Environment.ExitCode = evolutionBuild.Coverage.Complete ? 0 : 1;
        return;
    }
    RequireComplete(evolutionBuild);
    string reviewedCoverage = Path.Combine(root, "tools/CatalogGenerator/evolution-coverage.json");
    if (args.Contains("--verify-evolutions"))
    {
        if (JsonSerializer.Serialize(existing.Evolutions, TrackerJson.Options) != JsonSerializer.Serialize(evolutionBuild.Evolutions, TrackerJson.Options) ||
            !File.Exists(reviewedCoverage) || await File.ReadAllTextAsync(reviewedCoverage) != CoverageJson(evolutionBuild.Coverage))
            throw new InvalidDataException("Evolution data or coverage is stale. Complete the coverage worklist and regenerate both files.");
        Console.WriteLine("Evolution catalog and complete coverage inventory match regeneration.");
        return;
    }
    await File.WriteAllTextAsync(reviewedCoverage, CoverageJson(evolutionBuild.Coverage));
    existing = existing with { Evolutions = evolutionBuild.Evolutions };
    await File.WriteAllTextAsync(catalogPath, JsonSerializer.Serialize(existing, TrackerJson.Options));
    Console.WriteLine($"Evolution rules written: {existing.Evolutions.Values.Sum(edges => edges.Count)} game-specific rules.");
    return;
}
Dictionary<int, string> serebiiSlugs = [];
foreach (string era in new[] { "swsh", "sv" })
{
    string path = Path.Combine(cache, era + "-index.html");
    await Download($"https://www.serebii.net/pokedex-{era}/pikachu/", path);
}
Dictionary<int, string> slugs = Csv("pokemon_species").ToDictionary(row => Number(row["id"]), row => row["identifier"]);
Dictionary<int, string> names = Csv("pokemon_species_names").Where(row => row["local_language_id"] == "9")
    .ToDictionary(row => Number(row["pokemon_species_id"]), row => row["name"]);
var speciesByName = names.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);
foreach (string era in new[] { "swsh", "sv" })
    foreach (Match option in Regex.Matches(await File.ReadAllTextAsync(Path.Combine(cache, era + "-index.html"), System.Text.Encoding.Latin1), $"<option value=\"/pokedex-{era}/([^\"]+)/\">([^<]+)</option>"))
    {
        string name = Regex.Replace(Text(option.Groups[2].Value), @"^\d+\s+", "");
        if (speciesByName.TryGetValue(name, out int species)) serebiiSlugs[species] = option.Groups[1].Value;
    }
List<Dictionary<string, string>> rawPokemon = Csv("pokemon");
Dictionary<int, Dictionary<string, string>> defaults = rawPokemon.Where(row => row["is_default"] == "1").ToDictionary(row => Number(row["species_id"]));
Dictionary<string, PokemonVariant> pokemon = defaults.ToDictionary(pair => slugs[pair.Key], pair => new PokemonVariant
{
    Id = slugs[pair.Key], Name = names[pair.Key], NationalNumber = pair.Key, SpriteId = Number(pair.Value["id"])
});
foreach (var row in rawPokemon.Where(row => (row["identifier"].Contains("-alola") || row["identifier"].Contains("-galar") ||
    row["identifier"].Contains("-hisui") || row["identifier"] == "wooper-paldea" || row["identifier"] == "basculin-white-striped" ||
    row["identifier"].StartsWith("tauros-paldea-")) && !row["identifier"].Contains("totem") &&
    !row["identifier"].Contains("zen") && !row["identifier"].Contains("cap")))
{
    string id = row["identifier"].Replace("-standard", "").Replace("-combat-breed", "");
    int species = Number(row["species_id"]);
    string form = id.Contains("-alola") ? "Alolan" : id.Contains("-galar") ? "Galarian" : id.Contains("-hisui") ? "Hisuian" : id.Contains("-blaze-breed") ? "Paldean Blaze Breed" : id.Contains("-aqua-breed") ? "Paldean Aqua Breed" : id.Contains("-paldea") ? "Paldean" : "White-striped";
    pokemon[id] = new() { Id = id, Name = names[species], NationalNumber = species, SpriteId = Number(row["id"]), Form = form };
}
List<GameDefinition> games = [
    new("letsgo-pikachu", "Let's Go, Pikachu!", "Let's Go"), new("letsgo-eevee", "Let's Go, Eevee!", "Let's Go"),
    new("sword", "Sword", "Sword / Shield"), new("shield", "Shield", "Sword / Shield"),
    new("brilliant-diamond", "Brilliant Diamond", "Brilliant Diamond / Shining Pearl"), new("shining-pearl", "Shining Pearl", "Brilliant Diamond / Shining Pearl"),
    new("legends-arceus", "Legends: Arceus", "Legends: Arceus"),
    new("scarlet", "Scarlet", "Scarlet / Violet"), new("violet", "Violet", "Scarlet / Violet"),
    new("legends-za", "Legends: Z-A", "Legends: Z-A"),
    new("firered", "FireRed", "FireRed / LeafGreen"), new("leafgreen", "LeafGreen", "FireRed / LeafGreen"),
    new("home", "Pokémon HOME", "National")];
List<DexDefinition> dexes = [];
Add("letsgo-kanto", "Kanto", "Let's Go", 26);
Add("galar", "Galar", "Sword / Shield", 27, "galar");
Add("isle-of-armor", "Isle of Armor", "Sword / Shield", 28, "galar");
Add("crown-tundra", "Crown Tundra", "Sword / Shield", 29, "galar");
Add("sinnoh", "Sinnoh", "Brilliant Diamond / Shining Pearl", 5);
AddRange("bdsp-national", "National", "Brilliant Diamond / Shining Pearl", 493);
Add("hisui", "Hisui", "Legends: Arceus", 30, "hisui");
Add("paldea", "Paldea", "Scarlet / Violet", 31, "paldea");
Add("kitakami", "Kitakami", "Scarlet / Violet", 32);
Add("blueberry", "Blueberry", "Scarlet / Violet", 33);
Add("lumiose", "Lumiose", "Legends: Z-A", 34);
Add("hyperspace", "Hyperspace", "Legends: Z-A", 35);
Add("kanto", "Kanto", "FireRed / LeafGreen", 2);
AddRange("frlg-national", "National", "FireRed / LeafGreen", 386);
AddRange("national", "National", "National", names.Keys.Max());

Replace("hisui", "basculin", "basculin-white-striped");
Replace("kitakami", "basculin", "basculin-white-striped");
// Numbered membership is not proof that a Pokémon can originate in this release.
Find("letsgo-kanto").Entries.RemoveAll(e => e.PokemonId is "meltan" or "melmetal");
Find("bdsp-national").Entries.RemoveAll(e => e.PokemonId is "celebi" or "deoxys");
HashSet<int> unavailableFrlgJohto = [152, 153, 154, 155, 156, 157, 158, 159, 160, 163, 164, 170, 171, 179, 180, 181, 185, 190, 191, 192, 196, 197, 203, 204, 205, 207, 209, 210, 213, 216, 217, 222, 228, 229, 234, 235, 241, 251];
foreach (string id in new[] { "kanto", "frlg-national" })
    Find(id).Entries.RemoveAll(e => pokemon[e.PokemonId].NationalNumber is 151 || unavailableFrlgJohto.Contains(pokemon[e.PokemonId].NationalNumber) ||
        pokemon[e.PokemonId].NationalNumber is >= 252 and <= 385 and not (298 or 360));
foreach (string id in new[] { "grimer", "muk", "geodude", "graveler", "golem", "sandshrew", "sandslash", "vulpix", "ninetales", "diglett", "dugtrio" }) Replace("blueberry", id, id + "-alola");
foreach (string id in new[] { "slowpoke", "slowbro", "slowking" }) Replace("blueberry", id, id + "-galar");
Replace("blueberry", "qwilfish", "qwilfish-hisui");

// Explicit availability is reviewed independently of species-level dex membership.
string[] alolaLetsGo = "rattata raticate raichu sandshrew sandslash vulpix ninetales diglett dugtrio meowth persian geodude graveler golem grimer muk exeggutor marowak".Split(' ');
Extras("letsgo-kanto", alolaLetsGo.Select(id => id + "-alola"));
// Galarian Slowking requires the Crown Tundra's Galarica Wreath. The Isle's
// numbered slot can instead be filled by evolving its Diglett-reward Slowpoke.
Replace("isle-of-armor", "slowking-galar", "slowking");
Extras("galar", "meowth slowpoke-galar mr-mime yamask".Split(' '));
Extras("isle-of-armor", "raichu-alola sandshrew-alola sandslash-alola vulpix vulpix-alola ninetales ninetales-alola diglett diglett-alola dugtrio dugtrio-alola meowth meowth-alola meowth-galar persian persian-alola ponyta ponyta-galar rapidash rapidash-galar slowpoke slowbro farfetchd farfetchd-galar exeggutor-alola marowak-alola weezing weezing-galar mr-mime mr-mime-galar corsola corsola-galar zigzagoon zigzagoon-galar linoone linoone-galar darumaka darumaka-galar darmanitan darmanitan-galar stunfisk stunfisk-galar obstagoon perrserker cursola sirfetchd mr-rime".Split(' '));
Extras("crown-tundra", "raichu raichu-alola sandshrew sandshrew-alola sandslash sandslash-alola diglett diglett-alola dugtrio dugtrio-alola meowth meowth-alola meowth-galar persian persian-alola slowpoke slowpoke-galar slowbro farfetchd-galar marowak marowak-alola weezing weezing-galar mr-mime articuno zapdos moltres slowking slowking-galar corsola-galar zigzagoon linoone yamask yamask-galar cofagrigus stunfisk stunfisk-galar perrserker cursola sirfetchd runerigus".Split(' '));
Extras("hisui", new[] { "sneasel", "vulpix-alola", "ninetales-alola" });
// Availability is scoped to the game/DLC, not HOME compatibility or species membership.
Extras("paldea", new[] { "meowth-galar", "perrserker", "wooper", "quagsire" });
Extras("kitakami", new[] { "growlithe-hisui", "arcanine-hisui", "tauros" });
Extras("blueberry", new[] { "exeggutor-alola", "meowth-alola", "persian-alola" });
Extras("national", pokemon.Values.Where(p => p.Form != "").OrderBy(p => p.NationalNumber).ThenBy(p => p.Id).Select(p => p.Id));
Extras("lumiose", new[] { "raichu-alola", "slowpoke-galar", "slowbro-galar", "slowking-galar", "stunfisk-galar" });
Extras("hyperspace", new[] { "raichu-alola", "meowth-alola", "persian-alola", "marowak-alola", "meowth-galar", "slowpoke-galar", "slowbro-galar", "farfetchd-galar", "mr-mime-galar", "slowking-galar", "yamask-galar", "stunfisk-galar", "qwilfish-hisui", "sliggoo-hisui", "goodra-hisui", "avalugg-hisui" });
Combine("swsh-complete", "Complete", "Sword / Shield", "galar", "isle-of-armor", "crown-tundra");
Combine("sv-complete", "Complete", "Scarlet / Violet", "paldea", "kitakami", "blueberry");
Combine("za-complete", "Complete", "Legends: Z-A", "lumiose", "hyperspace");

dexes[dexes.FindIndex(d => d.Id == "bdsp-national")] = Find("bdsp-national") with { SourceDexIds = ["sinnoh", "bdsp-national"] };
dexes[dexes.FindIndex(d => d.Id == "frlg-national")] = Find("frlg-national") with { SourceDexIds = ["kanto", "frlg-national"] };
dexes[dexes.FindIndex(d => d.Id == "national")] = Find("national") with { SourceDexIds = dexes.SelectMany(d => d.SourceDexIds).Distinct().Order().ToList() };
string[] availabilityPages = ["https://www.serebii.net/swordshield/dynamaxadventurespokemon.shtml", "https://www.serebii.net/scarletviolet/snacksworthlegendary.shtml"];
var adventures = await ListedSpecies(availabilityPages[0], "swsh");
adventures.AddRange(["treecko", "torchic", "mudkip", "mew", "cosmog", "cosmoem", "poipole", "naganadel", "keldeo", "regigigas"]);
var snacksworth = await ListedSpecies(availabilityPages[1], "sv");
snacksworth.Add("urshifu");
Dictionary<string, List<string>> sourceOnly = new()
{
    ["Sword / Shield"] = adventures,
    ["Scarlet / Violet"] = snacksworth
};
Console.WriteLine($"Catalog: {games.Count} editions, {dexes.Count} lists, {pokemon.Count} identities. Fetching factual location records...");
var requests = games.Where(g => g.Id != "home").SelectMany(game => dexes.Where(d => d.Group == game.Group)
    .SelectMany(d => d.Entries).Select(e => pokemon[e.PokemonId])
    .Concat((sourceOnly.GetValueOrDefault(game.Group) ?? []).Select(id => pokemon[id]))
    .DistinctBy(p => p.Id).Select(p => (Game: game, Pokemon: p)))
    .GroupBy(pair => SourceUrl(pair.Game, pair.Pokemon)).ToList();
await Parallel.ForEachAsync(requests, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (group, _) =>
{
    string url = group.Key;
    string path = Path.Combine(cache, TrackerJson.Hash(url) + ".html");
    await Download(url, path);
    string html = await File.ReadAllTextAsync(path, System.Text.Encoding.Latin1);
    foreach (var pair in group)
    {
        var sources = AcquisitionExtractor.Extract(html, pair.Game, pair.Pokemon, dexes, pokemon, url);
        if (sources.Count > 0)
            lock (pair.Pokemon.Sources) pair.Pokemon.Sources[pair.Game.Id] = sources;
    }
});
Dictionary<string, string> letsGoTrades = new() { ["rattata"] = "Cerulean City", ["raticate"] = "Cerulean City", ["raichu"] = "Saffron City", ["sandshrew"] = "Celadon City", ["sandslash"] = "Celadon City", ["vulpix"] = "Celadon City", ["ninetales"] = "Celadon City", ["diglett"] = "Lavender Town", ["dugtrio"] = "Lavender Town", ["meowth"] = "Cinnabar Island", ["persian"] = "Cinnabar Island", ["geodude"] = "Vermilion City", ["graveler"] = "Vermilion City", ["golem"] = "Vermilion City", ["grimer"] = "Cinnabar Island", ["muk"] = "Cinnabar Island", ["exeggutor"] = "Indigo Plateau", ["marowak"] = "Fuchsia City" };
foreach (var p in pokemon.Values.Where(p => p.Form == "Alolan"))
{
    string species = slugs[p.NationalNumber];
    foreach (var game in games.Where(g => g.Group == "Let's Go"))
    {
        bool otherEdition = game.Id == "letsgo-pikachu" && new[] { "vulpix", "ninetales", "meowth", "persian" }.Contains(species) ||
            game.Id == "letsgo-eevee" && new[] { "sandshrew", "sandslash", "grimer", "muk" }.Contains(species);
        SetSource(game.Id, p.Id, "letsgo-kanto", otherEdition ? [] : [letsGoTrades[species]], otherEdition ? "Trade from the other Let's Go edition" : "Alolan trade / evolution");
    }
    foreach (var game in games.Where(g => g.Group == "Sword / Shield"))
    {
        SetSource(game.Id, p.Id, "isle-of-armor", ["Fields of Honor"], "Diglett reward / breeding or evolution");
        if (new[] { "raichu", "sandshrew", "sandslash", "diglett", "dugtrio", "meowth", "persian", "marowak" }.Contains(species))
            SetSource(game.Id, p.Id, "crown-tundra", ["Max Lair"], "Dynamax Adventures / breeding or evolution");
    }
}
foreach (var game in games.Where(g => g.Group is "Let's Go" or "Sword / Shield"))
    SetSource(game.Id, "mew", game.Group == "Let's Go" ? "letsgo-kanto" : "galar", [], "Poké Ball Plus Mystery Gift");
foreach (var game in games.Where(g => g.Group == "FireRed / LeafGreen"))
    foreach (string id in new[] { "lugia", "ho-oh", "deoxys" })
        SetSource(game.Id, id, "frlg-national", [id == "deoxys" ? "Birth Island" : "Navel Rock"], "Switch ticket encounter after entering the Hall of Fame", "https://www.serebii.net/fireredleafgreen/nintendoswitch.shtml");
foreach (string id in new[] { "vulpix-alola", "ninetales-alola" })
    SetSource("legends-arceus", id, "hisui", ["Snowfields Camp"], id == "vulpix-alola" ? "Request 83 reward" : "Evolve Alolan Vulpix");
foreach (var pair in new[] { ("raichu-alola", "Outside Quasartico Inc.", "In-game trade"), ("slowpoke-galar", "South Boulevard", "In-game trade"), ("stunfisk-galar", "Wild Zone 11", "Side Mission 72 reward") })
{
    SetSource("legends-za", pair.Item1, "lumiose", [pair.Item2], pair.Item3);
    SetSource("legends-za", pair.Item1, "hyperspace", ["Hyperspace Lumiose"], "Encounter");
}
foreach (string id in new[] { "slowbro-galar", "slowking-galar" })
    foreach (string scope in new[] { "lumiose", "hyperspace" })
        SetSource("legends-za", id, scope, [], "Evolve Galarian Slowpoke");
foreach (string gameId in new[] { "brilliant-diamond", "shining-pearl" })
{
    SetSource(gameId, "bellossom", "bdsp-national", [], "Evolve Gloom");
    foreach (string id in new[] { "manaphy", "darkrai", "shaymin" })
        SetSource(gameId, id, id == "manaphy" ? "sinnoh" : "bdsp-national", [], "Past in-game Mystery Gift / trade");
    SetSource(gameId, "phione", "bdsp-national", [], "Breed Manaphy with Ditto");
}
foreach (string gameId in new[] { "scarlet", "violet" })
{
    SetSource(gameId, "gimmighoul", "paldea", ["Watchtowers and ruins across Paldea"], "Chest Form encounter");
    SetSource(gameId, "wooper", "paldea", ["Cascarrafa"], "In-game trade");
    string academy = gameId == "scarlet" ? "Naranja Academy" : "Uva Academy";
    SetSource(gameId, "meowth-galar", "paldea", [academy], "Salvatore's gift");
    SetSource(gameId, "perrserker", "paldea", [], "Evolve Galarian Meowth");
    SetSource(gameId, "meowth-alola", "blueberry", ["League Club Room"], "Trade with Salvatore");
    SetSource(gameId, "persian-alola", "blueberry", [], "Evolve Alolan Meowth");
    SetSource(gameId, "tauros", "kitakami", [], "Breed Paldean Tauros in Kitakami");
    SetSource(gameId, "tauros", "blueberry", ["Savanna Biome"], "Encounter");
    foreach (string breed in new[] { "blaze", "aqua" })
    {
        bool nativeEdition = breed == "blaze" ? gameId == "scarlet" : gameId == "violet";
        SetSource(gameId, $"tauros-paldea-{breed}-breed", "paldea", nativeEdition ? ["Asado Desert", "East Province Area Two", "East Province Area Three", "West Province Area Two"] : [],
            nativeEdition ? "Encounter" : $"Trade from {(breed == "blaze" ? "Scarlet" : "Violet")}");
    }
    SetSource(gameId, "growlithe-hisui", "kitakami", ["Mossui Town"], "Perrin's reward");
    SetSource(gameId, "arcanine-hisui", "kitakami", [], "Evolve Hisuian Growlithe with a Fire Stone");
}
foreach (var game in games.Where(g => g.Group == "Sword / Shield"))
{
    foreach (var p in pokemon.Values.Where(p => p.Form.Length == 0 && pokemon.ContainsKey(p.Id + "-galar") && p.Sources.ContainsKey(game.Id)))
    {
        // These counterparts have different origins in each content scope.
        p.Sources[game.Id] = [];
        string? baseTrade = p.Id switch { "meowth" => "Turffield", "mr-mime" => "Spikemuth", "yamask" => "Ballonlea", _ => null };
        if (baseTrade is not null) SetSource(game.Id, p.Id, "galar", [baseTrade], "In-game trade");
        if (p.Id is "slowpoke" or "slowbro" or "slowking")
            SetSource(game.Id, p.Id, "isle-of-armor", ["Fields of Honor"], "Diglett reward / breeding or evolution");
        else if (p.Id is not ("meowth" or "articuno" or "zapdos" or "moltres" or "yamask"))
            SetSource(game.Id, p.Id, "isle-of-armor", ["Isle of Armor"], "Trade with Regina / breeding or evolution");
        if (p.Id is "slowpoke" or "slowbro" or "slowking" or "weezing" or "articuno" or "zapdos" or "moltres" or "stunfisk" or "mr-mime")
            SetSource(game.Id, p.Id, "crown-tundra", ["Max Lair"], "Dynamax Adventures / breeding or evolution");
        if (p.Id is "meowth" or "persian") SetSource(game.Id, p.Id, "crown-tundra", [], "Breed or evolve native Alolan Meowth");
    }
    SetSource(game.Id, "slowpoke-galar", "galar", ["Wedgehurst Station"], "Encounter");
    SetSource(game.Id, "slowpoke-galar", "isle-of-armor", ["Isle of Armor"], "Encounter");
    SetSource(game.Id, "slowking-galar", "crown-tundra", [], "Evolve Galarian Slowpoke with the Crown Tundra's Galarica Wreath");
    foreach (var pair in new[] { ("articuno-galar", "Crown Tundra"), ("zapdos-galar", "Wild Area"), ("moltres-galar", "Isle of Armor") })
    {
        pokemon[pair.Item1].Sources[game.Id] = [];
        SetSource(game.Id, pair.Item1, "crown-tundra", [pair.Item2], "Crown Tundra quest: roaming encounter after Dyna Tree Hill");
    }
}
// Missing route data stays visible without borrowing another Dex's locations.
foreach (var game in games.Where(g => g.Id != "home"))
    foreach (var dex in dexes.Where(d => d.Group == game.Group && d.SourceDexIds.Contains(d.Id)))
        foreach (var entry in dex.Entries)
            if (!pokemon[entry.PokemonId].Sources.TryGetValue(game.Id, out var records) || !records.Keys.Any(dex.SourceDexIds.Contains))
                SetSource(game.Id, entry.PokemonId, dex.Id, [], "See Serebii for availability");
Catalog catalog = new() { Version = "2026-10-06.1", Games = games, Pokemon = pokemon.Values.OrderBy(p => p.NationalNumber).ThenBy(p => p.Id).Select(p => p with { Sources = p.Sources.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value) }).ToList(), Dexes = dexes };
var fullEvolutionBuild = await BuildEvolutions(catalog);
RequireComplete(fullEvolutionBuild);
await File.WriteAllTextAsync(Path.Combine(root, "tools/CatalogGenerator/evolution-coverage.json"), CoverageJson(fullEvolutionBuild.Coverage));
catalog = catalog with { Evolutions = fullEvolutionBuild.Evolutions };
await File.WriteAllTextAsync(Path.Combine(output, "data/catalog.json"), JsonSerializer.Serialize(catalog, TrackerJson.Options));
await Parallel.ForEachAsync(catalog.Pokemon, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (p, _) =>
{
    string path = Path.Combine(spriteCache, p.SpriteId + ".png");
    await Download($"https://raw.githubusercontent.com/PokeAPI/sprites/{spriteCommit}/sprites/pokemon/{p.SpriteId}.png", path);
    File.Copy(path, Path.Combine(output, "sprites", p.SpriteId + ".png"), overwrite: true);
});
await File.WriteAllTextAsync(Path.Combine(output, "data/provenance.json"), JsonSerializer.Serialize(new { GeneratedUtc = DateTimeOffset.UtcNow, PokeApiCommit = dataCommit, SpritesCommit = spriteCommit, Sources = requests.Select(g => g.Key).Concat(availabilityPages).Concat(catalog.Pokemon.SelectMany(p => p.Sources.Values).SelectMany(sources => sources.Values).Select(source => source.Url)).Distinct().Order().ToArray() }, TrackerJson.Options));
Console.WriteLine("Catalog and local sprites written.");

string CoverageJson(EvolutionCoverageReport report) => JsonSerializer.Serialize(report, new JsonSerializerOptions(TrackerJson.Options) { WriteIndented = true });
async Task<EvolutionBuildResult> BuildEvolutions(Catalog catalog)
{
    var decisions = JsonSerializer.Deserialize<List<EvolutionDecision>>(await File.ReadAllTextAsync(Path.Combine(root, "tools/CatalogGenerator/evolution-decisions.json")), TrackerJson.Options)!;
    var result = EvolutionBuilder.Build(catalog, Csv, decisions, $"https://github.com/PokeAPI/pokeapi/blob/{dataCommit}/data/v2/csv/pokemon_evolution.csv");
    string reportPath = Path.Combine(root, "artifacts/evolution-coverage.json");
    await File.WriteAllTextAsync(reportPath, CoverageJson(result.Coverage));
    Console.WriteLine($"Evolution inventory: {result.Coverage.Paths.Count(p => p.Status == EvolutionCoverageStatus.Included)} included, {result.Coverage.Paths.Count(p => p.Status == EvolutionCoverageStatus.Unavailable)} unavailable, {result.Coverage.Paths.Count(p => p.Status == EvolutionCoverageStatus.Unresolved)} unresolved. Report: {reportPath}");
    foreach (string issue in result.Coverage.Issues) Console.WriteLine("RESEARCH: " + issue);
    foreach (var path in result.Coverage.Paths.Where(p => p.Status == EvolutionCoverageStatus.Unresolved))
        Console.WriteLine($"RESEARCH: {path.GameId}: {path.FromId} -> {path.ToId}: {path.Reason}");
    return result;
}
void RequireComplete(EvolutionBuildResult result)
{
    if (!result.Coverage.Complete)
        throw new InvalidDataException("Complete the evolution research worklist in artifacts/evolution-coverage.json, add the missing methods or cited decisions, and rerun. The existing catalog has not been replaced.");
}

int Number(string value) => int.Parse(value, CultureInfo.InvariantCulture);
async Task<List<string>> ListedSpecies(string url, string era)
{
    string path = Path.Combine(cache, TrackerJson.Hash(url) + ".html");
    await Download(url, path);
    string html = await File.ReadAllTextAsync(path, System.Text.Encoding.Latin1);
    var speciesBySlug = slugs.Values.ToDictionary(id => SerebiiSlug(pokemon[id]), id => id);
    return Regex.Matches(html, $"href=\"/pokedex-{era}/([^\"/]+)/?\"")
        .Select(match => speciesBySlug.GetValueOrDefault(match.Groups[1].Value)).OfType<string>()
        .Distinct(StringComparer.Ordinal).ToList();
}
List<Dictionary<string, string>> Csv(string name)
{
    using TextFieldParser parser = new(Path.Combine(dataCache, name + ".csv"));
    parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
    string[] headers = parser.ReadFields()!;
    List<Dictionary<string, string>> result = [];
    while (!parser.EndOfData) { string[] fields = parser.ReadFields()!; result.Add(headers.Select((header, i) => (header, fields[i])).ToDictionary(pair => pair.header, pair => pair.Item2)); }
    return result;
}
DexDefinition Find(string id) => dexes.Single(d => d.Id == id);
void Add(string id, string name, string group, int pokedex, string native = "")
{
    List<DexEntry> entries = Csv("pokemon_dex_numbers").Where(row => Number(row["pokedex_id"]) == pokedex).OrderBy(row => Number(row["pokedex_number"]))
        .Select(row => { string species = slugs[Number(row["species_id"])]; string variant = species + "-" + native; return new DexEntry(pokemon.ContainsKey(variant) ? variant : species, Number(row["pokedex_number"]), Region: name); }).ToList();
    dexes.Add(new() { Id = id, Name = name, Group = group, Entries = entries, SourceDexIds = [id] });
}
void AddRange(string id, string name, string group, int count) => dexes.Add(new() { Id = id, Name = name, Group = group, Entries = Enumerable.Range(1, count).Select(n => new DexEntry(slugs[n], n, Region: name)).ToList(), SourceDexIds = [id] });
void Replace(string dex, string original, string replacement) { var d = Find(dex); int index = d.Entries.FindIndex(e => e.PokemonId == original); if (index >= 0) d.Entries[index] = d.Entries[index] with { PokemonId = replacement }; }
void Extras(string dex, IEnumerable<string> ids) { var d = Find(dex); HashSet<string> present = d.Entries.Select(e => e.PokemonId).ToHashSet(); foreach (string id in ids) if (pokemon.ContainsKey(id) && present.Add(id)) d.Entries.Add(new(id, null, true, "Extra forms")); }
void Combine(string id, string name, string group, params string[] components) => dexes.Add(new() { Id = id, Name = name, Group = group, Entries = DexComposition.Combine(components.Select(Find)), SourceDexIds = components.ToList() });
string SourceUrl(GameDefinition game, PokemonVariant p) => game.Group switch
{
    "FireRed / LeafGreen" => $"https://www.serebii.net/pokedex-rs/{p.NationalNumber:D3}.shtml",
    "Let's Go" => $"https://www.serebii.net/pokedex-sm/{p.NationalNumber:D3}.shtml",
    "Sword / Shield" or "Brilliant Diamond / Shining Pearl" or "Legends: Arceus" => $"https://www.serebii.net/pokedex-swsh/{SerebiiSlug(p)}/",
    _ => $"https://www.serebii.net/pokedex-sv/{SerebiiSlug(p)}/"
};
string SerebiiSlug(PokemonVariant p) => serebiiSlugs.TryGetValue(p.NationalNumber, out string? slug) ? slug : slugs[p.NationalNumber] switch
{
    "farfetchd" => "farfetch'd", "sirfetchd" => "sirfetch'd", "mr-mime" => "mr.mime", "mr-rime" => "mr.rime", "mime-jr" => "mimejr.", "type-null" => "type:null", "nidoran-f" => "nidoranf", "nidoran-m" => "nidoranm", _ => slugs[p.NationalNumber]
};
string Text(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), @"\s+", " ").Trim();
async Task Download(string url, string path)
{
    if (File.Exists(path)) return;
    for (int attempt = 0; attempt < 4; attempt++)
    {
        try { byte[] bytes = await client.GetByteArrayAsync(url); await File.WriteAllBytesAsync(path, bytes); return; }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { throw new InvalidDataException($"Missing source: {url}", ex); }
        catch (HttpRequestException) when (attempt < 3) { await Task.Delay(1000 * (attempt + 1)); }
    }
}

void SetSource(string gameId, string pokemonId, string dexId, List<string> areas, string method, string? url = null)
{
    var p = pokemon[pokemonId];
    if (!p.Sources.TryGetValue(gameId, out var sources)) p.Sources[gameId] = sources = [];
    sources[dexId] = new(areas, method, url ?? SourceUrl(games.Single(g => g.Id == gameId), p));
}
