using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using PokedexTracker.Core;

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
foreach (string name in new[] { "pokemon", "pokemon_species", "pokemon_species_names", "pokemon_dex_numbers" })
    await Download($"https://raw.githubusercontent.com/PokeAPI/pokeapi/{dataCommit}/data/v2/csv/{name}.csv", Path.Combine(cache, name + ".csv"));
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
foreach (string id in new[] { "grimer", "muk", "geodude", "graveler", "golem", "sandshrew", "sandslash", "vulpix", "ninetales", "diglett", "dugtrio" }) Replace("blueberry", id, id + "-alola");
foreach (string id in new[] { "slowpoke", "slowbro", "slowking" }) Replace("blueberry", id, id + "-galar");
Replace("blueberry", "qwilfish", "qwilfish-hisui");

// Explicit availability is reviewed independently of species-level dex membership.
string[] alolaLetsGo = "rattata raticate raichu sandshrew sandslash vulpix ninetales diglett dugtrio meowth persian geodude graveler golem grimer muk exeggutor marowak".Split(' ');
Extras("letsgo-kanto", alolaLetsGo.Select(id => id + "-alola"));
string[] alolaSwsh = "raichu sandshrew sandslash vulpix ninetales diglett dugtrio meowth persian exeggutor marowak".Split(' ');
foreach (string dexId in new[] { "galar", "isle-of-armor", "crown-tundra" })
{
    var species = Find(dexId).Entries.Select(entry => pokemon[entry.PokemonId].NationalNumber).ToHashSet();
    Extras(dexId, pokemon.Values.Where(p => species.Contains(p.NationalNumber) && (p.Form == "" || p.Form == "Galarian" || alolaSwsh.Contains(slugs[p.NationalNumber]) && p.Form == "Alolan")).Select(p => p.Id));
}
Extras("hisui", new[] { "sneasel", "vulpix-alola", "ninetales-alola" });
Extras("paldea", new[] { "meowth-galar", "perrserker", "wooper", "quagsire", "tauros-paldea-blaze-breed", "tauros-paldea-aqua-breed" });
var svCompatible = pokemon.Values.Where(p => p.Form.StartsWith("Paldean") || p.Form == "Hisuian" || p.Id == "basculin-white-striped" ||
    p.Form == "Alolan" && !new[] { "rattata", "raticate", "marowak" }.Contains(slugs[p.NationalNumber]) ||
    p.Form == "Galarian" && new[] { "meowth", "slowpoke", "slowbro", "slowking", "weezing", "articuno", "zapdos", "moltres" }.Contains(slugs[p.NationalNumber])).ToList();
Dictionary<string, string> svEvolutions = new() { ["meowth-galar"] = "perrserker", ["qwilfish-hisui"] = "overqwil", ["sneasel-hisui"] = "sneasler", ["basculin-white-striped"] = "basculegion", ["wooper-paldea"] = "clodsire", ["wooper"] = "quagsire" };
foreach (string dexId in new[] { "paldea", "kitakami", "blueberry" })
{
    var species = Find(dexId).Entries.Select(entry => pokemon[entry.PokemonId].NationalNumber).ToHashSet();
    var eligible = svCompatible.Where(p => species.Contains(p.NationalNumber)).Select(p => p.Id)
        .Concat(Find(dexId).Entries.Where(e => pokemon[e.PokemonId].Form.Length > 0).Select(e => slugs[pokemon[e.PokemonId].NationalNumber])).Distinct().ToList();
    Extras(dexId, eligible);
    Extras(dexId, eligible.Where(svEvolutions.ContainsKey).Select(id => svEvolutions[id]));
}
Extras("national", pokemon.Values.Where(p => p.Form != "").OrderBy(p => p.NationalNumber).ThenBy(p => p.Id).Select(p => p.Id));
Extras("lumiose", new[] { "raichu-alola", "slowpoke-galar", "slowbro-galar", "slowking-galar", "stunfisk-galar", "sliggoo-hisui", "goodra-hisui", "avalugg-hisui" });
Extras("hyperspace", new[] { "raichu-alola", "meowth-alola", "persian-alola", "marowak-alola", "meowth-galar", "slowpoke-galar", "slowbro-galar", "farfetchd-galar", "mr-mime-galar", "slowking-galar", "yamask-galar", "stunfisk-galar", "qwilfish-hisui", "sliggoo-hisui", "goodra-hisui", "avalugg-hisui" });
Combine("swsh-complete", "Complete", "Sword / Shield", "galar", "isle-of-armor", "crown-tundra");
Combine("sv-complete", "Complete", "Scarlet / Violet", "paldea", "kitakami", "blueberry");
Combine("za-complete", "Complete", "Legends: Z-A", "lumiose", "hyperspace");

Console.WriteLine($"Catalog: {games.Count} editions, {dexes.Count} lists, {pokemon.Count} identities. Fetching factual location records...");
var requests = games.Where(g => g.Id != "home").SelectMany(game => dexes.Where(d => d.Group == game.Group)
    .SelectMany(d => d.Entries).Select(e => pokemon[e.PokemonId]).DistinctBy(p => p.Id).Select(p => (Game: game, Pokemon: p)))
    .GroupBy(pair => SourceUrl(pair.Game, pair.Pokemon)).ToList();
await Parallel.ForEachAsync(requests, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (group, _) =>
{
    string url = group.Key;
    string path = Path.Combine(cache, TrackerJson.Hash(url) + ".html");
    await Download(url, path);
    string html = await File.ReadAllTextAsync(path, System.Text.Encoding.Latin1);
    var tables = Regex.Matches(html, @"<table\b[^>]*>.*?</table>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
        .Select(match => match.Value).Where(table => Regex.IsMatch(table, @"Locations</h2>|<b>Location</b>", RegexOptions.IgnoreCase)).ToList();
    foreach (var pair in group)
    {
        List<string> areas = [];
        HashSet<string> methods = [];
        foreach (string table in tables)
        {
        string region = "paldea";
        foreach (Match row in Regex.Matches(table, @"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            var cells = Regex.Matches(row.Value, @"<td\b([^>]*)>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase).ToList();
            if (cells.Any(cell => Text(cell.Groups[2].Value) == "The Teal Mask")) region = "kitakami";
            if (cells.Any(cell => Text(cell.Groups[2].Value) == "The Indigo Disk")) region = "blueberry";
            if (cells.Count < 2 || !cells.Any(cell => MatchesGame(Text(cell.Groups[2].Value), pair.Game))) continue;
            var info = cells.FirstOrDefault(cell => cell.Groups[1].Value.Contains("fooinfo"));
            if (info is null) continue;
            string body = info.Groups[2].Value;
            string formName = pair.Pokemon.Form switch { "Alolan" => "Alolan", "Galarian" => "Galarian", "Hisuian" => "Hisuian", "Paldean" => "Paldean", _ => "" };
            var markers = Regex.Matches(body, @"(Alolan|Galarian|Hisuian|Paldean|Kantonian|Johtonian|Normal|Standard) Form(?:e)?(?:\s*:|\s*</b>)", RegexOptions.IgnoreCase).ToList();
            if (markers.Count > 0)
            {
                int marker = markers.FindIndex(m => formName.Length > 0 ? m.Groups[1].Value == formName : new[] { "Kantonian", "Johtonian", "Normal", "Standard" }.Contains(m.Groups[1].Value));
                if (marker >= 0) body = body[(markers[marker].Index + markers[marker].Length)..(marker + 1 < markers.Count ? markers[marker + 1].Index : body.Length)];
                else if (formName.Length == 0) body = body[..markers[0].Index];
                else continue;
                body = Regex.Split(body, @"(?:<br\s*/?>\s*)(?:Fixed:|Tera Raid Battles:|Max Raid Battles:)", RegexOptions.IgnoreCase)[0];
            }
            else if (pair.Game.Group == "Scarlet / Violet")
            {
                bool native = Find(region).Entries.Any(entry => !entry.Extra && entry.PokemonId == pair.Pokemon.Id) || region == "blueberry" && pair.Pokemon.Id == "exeggutor-alola";
                // Unlabelled rows describe the native form, not every compatible regional variant.
                if (!native && pair.Pokemon.Form.Length > 0 || !native && Find(region).Entries.Any(entry => !entry.Extra && pokemon[entry.PokemonId].NationalNumber == pair.Pokemon.NationalNumber && pokemon[entry.PokemonId].Form.Length > 0)) continue;
            }
            else if (pair.Pokemon.Form.Length > 0 && pair.Game.Id == "legends-za" ||
                pair.Pokemon.Form.Length > 0 && pair.Game.Group == "Sword / Shield" && pair.Pokemon.Form != "Galarian" ||
                pair.Pokemon.Form.Length > 0 && pair.Game.Group == "Let's Go" ||
                pair.Pokemon.Form == "Alolan" && pair.Game.Id == "legends-arceus") continue;
            foreach (Match anchor in Regex.Matches(body, "<a\\b[^>]*href=[\"']([^\"']+)[\"'][^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                if (anchor.Groups[1].Value.Contains("/pokearth/") || anchor.Groups[1].Value.Contains("/legendsz-a/wildzone") || anchor.Groups[1].Value.Contains("hyperspace"))
                {
                    string area = Text(anchor.Groups[2].Value);
                    if (area.Length > 0 && area != "Details" && area != "Map" && !anchor.Groups[1].Value.Contains("hyperspace/")) areas.Add(area);
                }
            string plain = Text(body);
            foreach (var rule in new[] { ("Evolve", "Evolution"), ("Trade", "Trade"), ("Transfer", "Transfer"), ("Gift", "Gift"), ("Given", "Gift"), ("Obtained", "Gift"), ("Starter", "Starter"), ("Event", "Event"), ("Raid", "Raid battles"), ("Breed", "Breeding"), ("Hatch", "Breeding"), ("Egg", "Breeding"), ("Not available", "Unavailable"), ("Not in", "Unavailable"), ("Fossil", "Fossil revival"), ("Revive", "Fossil revival"), ("Receive", "Gift"), ("Purchase", "Purchase"), ("Buy", "Purchase"), ("Coins", "Game Corner prize"), ("Fish Everywhere", "Fishing throughout Kanto") })
                if (plain.Contains(rule.Item1, StringComparison.OrdinalIgnoreCase)) methods.Add(rule.Item2);
            if (areas.Count == 0 && pair.Game.Group == "FireRed / LeafGreen")
            {
                foreach (Match place in Regex.Matches(plain, @"Route\s+\d+|(?:Pallet|Lavender|Fuchsia|Cerulean|Celadon|Saffron|Vermilion|Viridian|Pewter)\s+(?:Town|City)|(?:Viridian Forest|Safari Zone|Seafoam Islands|Pokémon Mansion|Power Plant|Victory Road|Mt\.? Moon|Rock Tunnel|Diglett'?s Cave|Cinnabar Island|Cerulean Cave|One Island|Two Island|Three Island|Four Island|Five Island|Six Island|Seven Island|Treasure Beach|Kindle Road|Mt\.? Ember|Icefall Cave|Pattern Bush|Altering Cave|Tanoby Ruins)", RegexOptions.IgnoreCase)) areas.Add(place.Value);
                foreach (Match routes in Regex.Matches(plain, @"Routes\s+([\d\s,&]+)"))
                    foreach (Match n in Regex.Matches(routes.Groups[1].Value, @"\d+")) areas.Add("Route " + n.Value);
                foreach (string place in new[] { "Bond Bridge", "Berry Forest", "Berry Bush", "Five Isle Meadow", "Cape Brink", "Water Path", "Diglett Cave", "Lost Cave", "Pokémon Tower", "Pokemon Tower", "Pokemon Mansion", "Celadon Mansion", "Celadon Game Corner", "Canyon Entrance", "Ruin Valley", "Dilford Chamber", "Liptoo Chamber", "Monean Chamber", "Rixy Chamber", "Scufib Chamber", "Viapolis Chamber", "Weepth Chamber", "Three Isle Port", "Seavault Canyon", "Navel Rock", "Birth Island" })
                    if (plain.Contains(place, StringComparison.OrdinalIgnoreCase)) areas.Add(place);
            }
            if (pair.Game.Id == "legends-za")
                foreach (string place in new[] { "Hyperspace Primordial Sea", "Hyperspace Desolate Land", "Hyperspace Sky Pillar", "Hyperspace Infernal Arena", "Hyperspace Newmoon Nightmare" })
                    if (plain.Contains(place, StringComparison.Ordinal)) areas.Add(place);
        }
        }
        if (areas.Count == 0 && methods.Count == 0 && pair.Pokemon.Form.Length > 0) methods.Add("Transfer / regional evolution");
        AcquisitionSource source = new(areas.Distinct().ToList(), methods.Count == 0 ? (areas.Count > 0 ? "Encounter" : "See Serebii for availability") : string.Join(" / ", methods.Order()), url);
        lock (pair.Pokemon.Sources) pair.Pokemon.Sources[pair.Game.Id] = source;
    }
});
Dictionary<string, string> letsGoTrades = new() { ["rattata"] = "Cerulean City", ["raticate"] = "Cerulean City", ["raichu"] = "Saffron City", ["sandshrew"] = "Celadon City", ["sandslash"] = "Celadon City", ["vulpix"] = "Celadon City", ["ninetales"] = "Celadon City", ["diglett"] = "Lavender Town", ["dugtrio"] = "Lavender Town", ["meowth"] = "Cinnabar Island", ["persian"] = "Cinnabar Island", ["geodude"] = "Vermilion City", ["graveler"] = "Vermilion City", ["golem"] = "Vermilion City", ["grimer"] = "Cinnabar Island", ["muk"] = "Cinnabar Island", ["exeggutor"] = "Indigo Plateau", ["marowak"] = "Fuchsia City" };
foreach (var p in pokemon.Values.Where(p => p.Form == "Alolan"))
{
    string species = slugs[p.NationalNumber];
    foreach (var game in games.Where(g => g.Group == "Let's Go"))
        if (p.Sources.ContainsKey(game.Id))
        {
            bool otherEdition = game.Id == "letsgo-pikachu" && new[] { "vulpix", "ninetales", "meowth", "persian" }.Contains(species) ||
                game.Id == "letsgo-eevee" && new[] { "sandshrew", "sandslash", "grimer", "muk" }.Contains(species);
            p.Sources[game.Id] = new(otherEdition ? [] : [letsGoTrades[species]], otherEdition ? "Trade from the other Let's Go edition" : "Alolan trade / evolution", SourceUrl(game, p));
        }
    foreach (var game in games.Where(g => g.Group == "Sword / Shield"))
        if (p.Sources.ContainsKey(game.Id)) p.Sources[game.Id] = new(["Fields of Honor"], "Isle of Armor Diglett reward / evolution, or transfer", SourceUrl(game, p));
}
foreach (string id in new[] { "vulpix-alola", "ninetales-alola" })
    pokemon[id].Sources["legends-arceus"] = new(["Snowfields Camp"], id == "vulpix-alola" ? "Request 83 reward" : "Evolve Alolan Vulpix", SourceUrl(games.Single(g => g.Id == "legends-arceus"), pokemon[id]));
pokemon["raichu-alola"].Sources["legends-za"] = new(["Lumiose City"], "In-game trade / Hyperspace Lumiose", SourceUrl(games.Single(g => g.Id == "legends-za"), pokemon["raichu-alola"]));
pokemon["slowpoke-galar"].Sources["legends-za"] = new(["Lumiose City", "Hyperspace Lumiose"], "In-game trade / encounter", SourceUrl(games.Single(g => g.Id == "legends-za"), pokemon["slowpoke-galar"]));
pokemon["stunfisk-galar"].Sources["legends-za"] = new(["Lumiose City", "Hyperspace Lumiose"], "Gift / encounter", SourceUrl(games.Single(g => g.Id == "legends-za"), pokemon["stunfisk-galar"]));
foreach (string gameId in new[] { "brilliant-diamond", "shining-pearl" })
    pokemon["bellossom"].Sources[gameId] = new([], "Evolve Gloom", SourceUrl(games.Single(g => g.Id == gameId), pokemon["bellossom"]));
foreach (string gameId in new[] { "scarlet", "violet" })
{
    pokemon["gimmighoul"].Sources[gameId] = new(["Watchtowers and ruins across Paldea"], "Chest Form encounter", SourceUrl(games.Single(g => g.Id == gameId), pokemon["gimmighoul"]));
    var ordinaryWooper = pokemon["wooper"].Sources[gameId];
    pokemon["wooper"].Sources[gameId] = ordinaryWooper with { Areas = ordinaryWooper.Areas.Where(area => area != "South Province Area One").Prepend("Cascarrafa").Distinct().ToList() };
}
foreach (var game in games.Where(g => g.Group == "Sword / Shield"))
    foreach (var p in pokemon.Values.Where(p => p.Form.Length == 0 && pokemon.ContainsKey(p.Id + "-galar") && p.Sources.ContainsKey(game.Id)))
        p.Sources[game.Id] = new(p.Id is "articuno" or "zapdos" or "moltres" ? ["Max Lair"] : p.Id == "mr-mime" ? ["Spikemuth", "Isle of Armor"] : [],
            p.Id is "articuno" or "zapdos" or "moltres" ? "Dynamax Adventures" : "Transfer / in-game trade / evolution", SourceUrl(game, p));
foreach (var game in games.Where(g => g.Group == "Sword / Shield"))
    foreach (var pair in new[] { ("articuno-galar", "Crown Tundra"), ("zapdos-galar", "Wild Area"), ("moltres-galar", "Isle of Armor") })
        pokemon[pair.Item1].Sources[game.Id] = new([pair.Item2], "Roaming encounter after Dyna Tree Hill", SourceUrl(game, pokemon[pair.Item1]));
// The Teal Mask reward is not a wild encounter of the ordinary form.
foreach (string game in new[] { "scarlet", "violet" })
{
    pokemon["growlithe-hisui"].Sources[game] = new(["Mossui Town"], "Perrin's reward", SourceUrl(games.Single(g => g.Id == game), pokemon["growlithe-hisui"]));
    pokemon["arcanine-hisui"].Sources[game] = new([], "Evolve Hisuian Growlithe", SourceUrl(games.Single(g => g.Id == game), pokemon["arcanine-hisui"]));
}
Catalog catalog = new() { Version = "2026-10-04.1", Games = games, Pokemon = pokemon.Values.OrderBy(p => p.NationalNumber).ThenBy(p => p.Id).Select(p => p with { Sources = p.Sources.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value) }).ToList(), Dexes = dexes };
await File.WriteAllTextAsync(Path.Combine(output, "data/catalog.json"), JsonSerializer.Serialize(catalog, TrackerJson.Options));
await Parallel.ForEachAsync(catalog.Pokemon, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (p, _) =>
    await Download($"https://raw.githubusercontent.com/PokeAPI/sprites/{spriteCommit}/sprites/pokemon/{p.SpriteId}.png", Path.Combine(output, "sprites", p.SpriteId + ".png")));
await File.WriteAllTextAsync(Path.Combine(output, "data/provenance.json"), JsonSerializer.Serialize(new { GeneratedUtc = DateTimeOffset.UtcNow, PokeApiCommit = dataCommit, SpritesCommit = spriteCommit, Sources = requests.Select(g => g.Key).Order().ToArray() }, TrackerJson.Options));
Console.WriteLine("Catalog and local sprites written.");

int Number(string value) => int.Parse(value, CultureInfo.InvariantCulture);
List<Dictionary<string, string>> Csv(string name)
{
    using TextFieldParser parser = new(Path.Combine(cache, name + ".csv"));
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
    dexes.Add(new() { Id = id, Name = name, Group = group, Entries = entries });
}
void AddRange(string id, string name, string group, int count) => dexes.Add(new() { Id = id, Name = name, Group = group, Entries = Enumerable.Range(1, count).Select(n => new DexEntry(slugs[n], n, Region: name)).ToList() });
void Replace(string dex, string original, string replacement) { var d = Find(dex); int index = d.Entries.FindIndex(e => e.PokemonId == original); if (index >= 0) d.Entries[index] = d.Entries[index] with { PokemonId = replacement }; }
void Extras(string dex, IEnumerable<string> ids) { var d = Find(dex); HashSet<string> present = d.Entries.Select(e => e.PokemonId).ToHashSet(); foreach (string id in ids) if (pokemon.ContainsKey(id) && present.Add(id)) d.Entries.Add(new(id, null, true, "Extra forms")); }
void Combine(string id, string name, string group, params string[] components) => dexes.Add(new() { Id = id, Name = name, Group = group, Entries = DexComposition.Combine(components.Select(Find)) });
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
bool MatchesGame(string label, GameDefinition game) => label.Equals(game.Name, StringComparison.OrdinalIgnoreCase) ||
    game.Id == "legends-za" && label.StartsWith("Legends: Z-A", StringComparison.OrdinalIgnoreCase) ||
    game.Id == "legends-arceus" && label.Replace(" ", "").Equals("Legends:Arceus", StringComparison.OrdinalIgnoreCase);
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
