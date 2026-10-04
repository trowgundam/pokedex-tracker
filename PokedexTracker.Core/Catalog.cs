namespace PokedexTracker.Core;

public sealed record Catalog
{
    public required string Version { get; init; }
    public required List<GameDefinition> Games { get; init; }
    public required List<PokemonVariant> Pokemon { get; init; }
    public required List<DexDefinition> Dexes { get; init; }
}

public sealed record GameDefinition(string Id, string Name, string Group);

public sealed record PokemonVariant
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int NationalNumber { get; init; }
    public required int SpriteId { get; init; }
    public string Form { get; init; } = "";
    public Dictionary<string, AcquisitionSource> Sources { get; init; } = [];
    public string DisplayName => Form.Length == 0 ? Name : $"{Form} {Name}";
}

public sealed record AcquisitionSource(List<string> Areas, string Method, string Url);

public sealed record DexDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required List<DexEntry> Entries { get; init; }
    public int Columns { get; init; } = 6;
    public int Rows { get; init; } = 5;
}

public sealed record DexEntry(string PokemonId, int? Number, bool Extra = false, string Region = "");

public static class DexComposition
{
    public static List<DexEntry> Combine(IEnumerable<DexDefinition> dexes)
    {
        List<DexDefinition> ordered = dexes.ToList();
        HashSet<string> seen = [];
        List<DexEntry> result = [];
        foreach (DexEntry entry in ordered.SelectMany(dex => dex.Entries.Where(entry => !entry.Extra)))
        {
            if (seen.Add(entry.PokemonId)) result.Add(entry);
        }
        foreach (DexEntry entry in ordered.SelectMany(dex => dex.Entries.Where(entry => entry.Extra)))
        {
            if (seen.Add(entry.PokemonId)) result.Add(entry);
        }
        return result;
    }
}
