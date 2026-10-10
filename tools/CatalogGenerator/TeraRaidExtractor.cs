using System.Net;
using System.Text.RegularExpressions;
using PokedexTracker.Core;

namespace PokedexTracker.CatalogGenerator;

public sealed record TeraRaidEntry(string PokemonId, string Scope, string GameId);

public static class TeraRaidExtractor
{
    public static List<TeraRaidEntry> Extract(string html, IReadOnlyCollection<PokemonVariant> variants)
    {
        List<TeraRaidEntry> entries = [];
        var headings = Regex.Matches(html, "<h2><a name=\"(paldea|kitakami|terarium)\"></a>", RegexOptions.IgnoreCase).ToList();
        var groups = Regex.Matches(html, "<tr>\\s*(?<sprites><td class=\"pkmn\">(?:(?!</tr>).)*?/scarletviolet/pokemon/(?:(?!</tr>).)*?)</tr>\\s*<tr>.*?</tr>\\s*<tr>(?<games>.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        int parsedSprites = 0;
        foreach (Match group in groups)
        {
            var heading = headings.LastOrDefault(h => h.Index < group.Index);
            if (heading is null) throw new InvalidDataException("Tera Raid entries have no region heading.");
            string scope = heading.Groups[1].Value == "terarium" ? "blueberry" : heading.Groups[1].Value;
            var sprites = Regex.Matches(group.Groups["sprites"].Value, @"/scarletviolet/pokemon/(?:new/)?small/(\d+)([^""/]*?)\.png").ToList();
            parsedSprites += sprites.Count;
            var games = Regex.Split(group.Groups["games"].Value, @"<td\b[^>]*>", RegexOptions.IgnoreCase).Skip(1).ToList();
            if (sprites.Count != games.Count) throw new InvalidDataException("Tera Raid images and edition columns do not match.");
            for (int i = 0; i < sprites.Count; i++)
            {
                int number = int.Parse(sprites[i].Groups[1].Value);
                string suffix = sprites[i].Groups[2].Value;
                string form = (number, suffix) switch
                {
                    (128, "-b") => "Paldean Blaze Breed",
                    (128, "-a") => "Paldean Aqua Breed",
                    (550, "-w") => "White-striped",
                    (_, "-a") => "Alolan",
                    (_, "-g") => "Galarian",
                    (_, "-h") => "Hisuian",
                    (_, "-p") => "Paldean",
                    _ => ""
                };
                var species = variants.Where(p => p.NationalNumber == number).ToList();
                var pokemon = species.FirstOrDefault(p => p.Form == form);
                // Cosmetic sprite suffixes share the ordinary checklist identity.
                if (pokemon is null && species.All(p => p.Form.Length == 0)) pokemon = species.SingleOrDefault();
                if (pokemon is null) throw new InvalidDataException($"Unknown Tera Raid identity: {number}{suffix}.");
                string edition = WebUtility.HtmlDecode(Regex.Replace(games[i], "<[^>]+>", " "));
                if (!Regex.IsMatch(edition, @"\b(Both|Scarlet|Violet)\b")) throw new InvalidDataException("Tera Raid entry has no edition.");
                foreach (string game in new[] { "scarlet", "violet" })
                    if (edition.Contains("Both", StringComparison.Ordinal) || edition.Contains(game == "scarlet" ? "Scarlet" : "Violet", StringComparison.Ordinal))
                        entries.Add(new(pokemon.Id, scope, game));
            }
        }
        if (parsedSprites != Regex.Matches(html, @"/scarletviolet/pokemon/(?:new/)?small/\d+[^""/]*?\.png").Count)
            throw new InvalidDataException("Some Tera Raid entries were not parsed.");
        if (entries.Count == 0) throw new InvalidDataException("No Tera Raid entries were extracted.");
        return entries.Distinct().ToList();
    }
}
