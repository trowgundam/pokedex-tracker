using System.Net;
using System.Text.RegularExpressions;

using PokedexTracker.Core;

namespace PokedexTracker.CatalogGenerator;

public static class AcquisitionExtractor
{
    public static Dictionary<string, AcquisitionSource> Extract(string html, GameDefinition game, PokemonVariant variant,
        IReadOnlyList<DexDefinition> dexes, IReadOnlyDictionary<string, PokemonVariant> variants, string url)
    {
        Dictionary<string, List<AcquisitionSource>> result = [];
        var tables = Regex.Matches(html, @"<table\b[^>]*>.*?</table>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .Select(match => match.Value).Where(table => Regex.IsMatch(table, @"Locations</h2>|<b>Location</b>", RegexOptions.IgnoreCase));
        foreach (string table in tables)
        {
            string region = game.Group switch
            {
                "Scarlet / Violet" => "paldea",
                "Sword / Shield" => "galar",
                "Let's Go" => "letsgo-kanto",
                "Brilliant Diamond / Shining Pearl" => "sinnoh",
                "Legends: Arceus" => "hisui",
                "Legends: Z-A" => "lumiose",
                "FireRed / LeafGreen" => "kanto",
                _ => throw new InvalidDataException("Unknown acquisition game group.")
            };
            foreach (Match row in Regex.Matches(table, @"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var cells = Regex.Matches(row.Value, @"<td\b([^>]*)>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase).ToList();
                if (game.Group == "Scarlet / Violet")
                {
                    if (cells.Any(cell => Text(cell.Groups[2].Value) == "The Teal Mask")) region = "kitakami";
                    if (cells.Any(cell => Text(cell.Groups[2].Value) == "The Indigo Disk")) region = "blueberry";
                }
                if (game.Group == "Sword / Shield")
                {
                    if (cells.Any(cell => Text(cell.Groups[2].Value) == "Isle of Armor")) region = "isle-of-armor";
                    if (cells.Any(cell => Text(cell.Groups[2].Value) == "Crown Tundra")) region = "crown-tundra";
                }
                if (cells.Count < 2 || !cells.Any(cell => MatchesGame(Text(cell.Groups[2].Value), game))) continue;
                var info = cells.FirstOrDefault(cell => cell.Groups[1].Value.Contains("fooinfo"));
                if (info is null) continue;
                if (game.Id == "legends-za") region = cells.Any(cell => Text(cell.Groups[2].Value).Contains("Mega Dimension", StringComparison.Ordinal)) ? "hyperspace" : "lumiose";
                List<string> areas = [];
                HashSet<string> methods = new(StringComparer.Ordinal);
                string body = info.Groups[2].Value;
                string formName = variant.Form switch { "Alolan" => "Alolan", "Galarian" => "Galarian", "Hisuian" => "Hisuian", "Paldean" => "Paldean", _ => "" };
                var markers = Regex.Matches(body, @"(Alolan|Galarian|Hisuian|Paldean|Kantonian|Johtonian|Normal|Standard) Form(?:e)?(?:\s*:|\s*</b>)", RegexOptions.IgnoreCase).ToList();
                if (markers.Count > 0)
                {
                    int marker = markers.FindIndex(m => formName.Length > 0 ? m.Groups[1].Value == formName : new[] { "Kantonian", "Johtonian", "Normal", "Standard" }.Contains(m.Groups[1].Value));
                    if (marker >= 0) body = body[(markers[marker].Index + markers[marker].Length)..(marker + 1 < markers.Count ? markers[marker + 1].Index : body.Length)];
                    else if (formName.Length == 0) body = body[..markers[0].Index];
                    else continue;
                    body = Regex.Split(body, @"(?:<br\s*/?>\s*)(?:Fixed:|Tera Raid Battles:|Max Raid Battles:)", RegexOptions.IgnoreCase)[0];
                }
                else if (game.Group == "Scarlet / Violet")
                {
                    bool native = dexes.Single(d => d.Id == region).Entries.Any(entry => !entry.Extra && entry.PokemonId == variant.Id) || region == "blueberry" && variant.Id == "exeggutor-alola";
                    // Unlabelled rows describe the native form, not every compatible regional variant.
                    if (!native && variant.Form.Length > 0 || !native && dexes.Single(d => d.Id == region).Entries.Any(entry => !entry.Extra && variants[entry.PokemonId].NationalNumber == variant.NationalNumber && variants[entry.PokemonId].Form.Length > 0)) continue;
                }
                else if (variant.Form.Length > 0 && game.Id == "legends-za" ||
                    variant.Form.Length > 0 && game.Group == "Sword / Shield" && variant.Form != "Galarian" ||
                    variant.Form.Length > 0 && game.Group == "Let's Go" ||
                    variant.Form == "Alolan" && game.Id == "legends-arceus") continue;
                bool numbered = dexes.Any(d => d.Id == region && d.Entries.Any(entry => !entry.Extra && entry.PokemonId == variant.Id));
                List<RaidEncounter> raids = [];
                var raidSections = Regex.Matches(info.Groups[2].Value, @"(Tera Raid Battles|Max Raid Battles|Gigantamax Raid Battles):\s*(.*?)(?=(?:<br\s*/?>\s*)?(?:Tera Raid Battles|Max Raid Battles|Gigantamax Raid Battles):|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match section in raidSections)
                {
                    string raidBody = section.Groups[2].Value;
                    var raidForms = Regex.Matches(raidBody, @"(Alolan|Galarian|Hisuian|Paldean|Kantonian|Johtonian|Normal|Standard) Form(?:e)?(?:\s*:|\s*</b>)", RegexOptions.IgnoreCase).ToList();
                    if (raidForms.Count > 0)
                    {
                        int marker = raidForms.FindIndex(m => formName.Length > 0 ? m.Groups[1].Value == formName : new[] { "Kantonian", "Johtonian", "Normal", "Standard" }.Contains(m.Groups[1].Value));
                        if (marker >= 0) raidBody = raidBody[(raidForms[marker].Index + raidForms[marker].Length)..(marker + 1 < raidForms.Count ? raidForms[marker + 1].Index : raidBody.Length)];
                        else if (formName.Length == 0) raidBody = raidBody[..raidForms[0].Index];
                        else continue;
                    }
                    // Unlabelled raid sections describe the scope's numbered native form.
                    else if (markers.Count > 0 && !numbered) continue;
                    foreach (var anchor in Anchors(raidBody))
                    {
                        string href = anchor.Href;
                        string detail = anchor.Text;
                        if (detail.Length == 0 || detail is "Details" or "Map") continue;
                        if (section.Groups[1].Value.Equals("Tera Raid Battles", StringComparison.OrdinalIgnoreCase) ? !href.Contains("/teraraidbattles/") : !href.Contains("/pokearth/")) continue;
                        raids.Add(new(section.Groups[1].Value, detail, new Uri(new Uri(url), href).AbsoluteUri));
                    }
                }
                body = Regex.Split(body, @"(?:<br\s*/?>\s*)?(?:Tera Raid Battles|Max Raid Battles|Gigantamax Raid Battles):", RegexOptions.IgnoreCase)[0];
                foreach (var anchor in Anchors(body))
                    if (anchor.Href.Contains("/pokearth/") || anchor.Href.Contains("/legendsz-a/wildzone") || anchor.Href.Contains("hyperspace"))
                    {
                        string area = anchor.Text;
                        if (area.Length > 0 && area != "Details" && area != "Map") areas.Add(area);
                    }
                string plain = Text(body);
                if (raids.Count > 0) methods.Add("Raid battles");
                if (game.Id == "legends-za" && (body.Contains("hyperspacewildzone/", StringComparison.Ordinal) || plain.Contains("Hyperspace Lumiose", StringComparison.Ordinal))) areas.Add("Hyperspace Lumiose");
                // Transfer and DLC-owner trades cannot establish a native source in this scope.
                if (areas.Count == 0 && Regex.IsMatch(plain, @"Transfer|Trade.*players.*(?:Teal Mask|Indigo Disk|Isle of Armor|Crown Tundra)|Not available|Not in", RegexOptions.IgnoreCase) &&
                    !Regex.IsMatch(plain, @"Evolve|Breed|Hatch|Gift|Given|Starter|Dynamax Adventures|Fossil|Receive", RegexOptions.IgnoreCase) &&
                    !(numbered && Regex.IsMatch(plain, @"Event|Raid", RegexOptions.IgnoreCase))) continue;
                if (game.Group == "Sword / Shield" && plain.Contains("Dynamax Adventures", StringComparison.OrdinalIgnoreCase))
                {
                    areas.Add("Max Lair");
                    methods.Add("Dynamax Adventures");
                }
                foreach (var rule in new[] { ("Evolve", "Evolution"), ("Trade", "Trade"), ("Gift", "Gift"), ("Given", "Gift"), ("Obtained", "Gift"), ("Starter", "Starter"), ("Event", "Event"), ("Raid", "Raid battles"), ("Breed", "Breeding"), ("Hatch", "Breeding"), ("Egg", "Breeding"), ("Fossil", "Fossil revival"), ("Revive", "Fossil revival"), ("Receive", "Gift"), ("Purchase", "Purchase"), ("Buy", "Purchase"), ("Coins", "Game Corner prize"), ("Fish Everywhere", "Fishing throughout Kanto") })
                    if (plain.Contains(rule.Item1, StringComparison.OrdinalIgnoreCase)) methods.Add(rule.Item2);
                if (areas.Count == 0 && game.Group == "FireRed / LeafGreen")
                {
                    foreach (Match place in Regex.Matches(plain, @"Route\s+\d+|(?:Pallet|Lavender|Fuchsia|Cerulean|Celadon|Saffron|Vermilion|Viridian|Pewter)\s+(?:Town|City)|(?:Viridian Forest|Safari Zone|Seafoam Islands|Pokémon Mansion|Power Plant|Victory Road|Mt\.? Moon|Rock Tunnel|Diglett'?s Cave|Cinnabar Island|Cerulean Cave|One Island|Two Island|Three Island|Four Island|Five Island|Six Island|Seven Island|Treasure Beach|Kindle Road|Mt\.? Ember|Icefall Cave|Pattern Bush|Altering Cave|Tanoby Ruins)", RegexOptions.IgnoreCase)) areas.Add(place.Value);
                    foreach (Match routes in Regex.Matches(plain, @"Routes\s+([\d\s,&]+)"))
                        foreach (Match n in Regex.Matches(routes.Groups[1].Value, @"\d+")) areas.Add("Route " + n.Value);
                    foreach (string place in new[] { "Bond Bridge", "Berry Forest", "Berry Bush", "Five Isle Meadow", "Cape Brink", "Water Path", "Diglett Cave", "Lost Cave", "Pokémon Tower", "Pokemon Tower", "Pokemon Mansion", "Celadon Mansion", "Celadon Game Corner", "Canyon Entrance", "Ruin Valley", "Dilford Chamber", "Liptoo Chamber", "Monean Chamber", "Rixy Chamber", "Scufib Chamber", "Viapolis Chamber", "Weepth Chamber", "Three Isle Port", "Seavault Canyon", "Navel Rock", "Birth Island" })
                        if (plain.Contains(place, StringComparison.OrdinalIgnoreCase)) areas.Add(place);
                }
                if (game.Id == "legends-za")
                    foreach (string place in new[] { "Hyperspace Primordial Sea", "Hyperspace Desolate Land", "Hyperspace Sky Pillar", "Hyperspace Infernal Arena", "Hyperspace Newmoon Nightmare" })
                        if (plain.Contains(place, StringComparison.Ordinal)) areas.Add(place);
                if (areas.Count > 0 && !methods.Any(method => method is "Evolution" or "Trade" or "Gift" or "Starter" or "Event" or "Breeding" or "Fossil revival" or "Purchase" or "Game Corner prize" or "Dynamax Adventures"))
                    methods.Add("Encounter");
                if (!numbered && methods.Contains("Event"))
                {
                    if (methods.Count == 1) continue;
                    methods.Remove("Event");
                }
                string method = methods.Count == 0 ? "See Serebii for availability" : string.Join(" / ", methods.Order(StringComparer.Ordinal));
                if (!result.TryGetValue(region, out var sources)) result[region] = sources = [];
                sources.Add(new(areas.Distinct(StringComparer.Ordinal).ToList(), method, url) { Raids = raids.Distinct().ToList() });
            }
        }
        return result.ToDictionary(pair => pair.Key, pair => AcquisitionSource.Combine(pair.Value));
    }
    private static bool MatchesGame(string label, GameDefinition game) => label.Equals(game.Name, StringComparison.OrdinalIgnoreCase) ||
        game.Id == "legends-za" && label.StartsWith("Legends: Z-A", StringComparison.OrdinalIgnoreCase) ||
        game.Id == "legends-arceus" && label.Replace(" ", "").Equals("Legends:Arceus", StringComparison.OrdinalIgnoreCase);
    private static IEnumerable<(string Href, string Text)> Anchors(string html) =>
        Regex.Matches(html, "<a\\b[^>]*href=(?:\"(?<href>[^\"]+)\"|'(?<href>[^']+)')[^>]*>(?<text>.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .Select(match => (WebUtility.HtmlDecode(match.Groups["href"].Value), Text(match.Groups["text"].Value)));
    private static string Text(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), @"\s+", " ").Trim();
}
