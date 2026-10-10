namespace PokedexTracker.Core;

public sealed record RankedLocation(string GameId, string? Area, List<PokemonVariant> Pokemon)
{
    public string? RaidMethod { get; init; }
}
public sealed record AcquisitionOverview(List<RankedLocation> Locations, List<PokemonVariant> WithoutLocation)
{
    public List<RankedLocation> Raids { get; init; } = [];
}

public static class AcquisitionRanking
{
    public static AcquisitionOverview Build(IEnumerable<PokemonVariant> outstanding, string gameId, DexDefinition dex)
    {
        var remaining = outstanding.DistinctBy(p => p.Id).ToList();
        var located = remaining.SelectMany(p => p.Sources.Keys
            .Where(id => gameId == "home" || id == gameId)
            .Select(id => (GameId: id, Source: p.SourceFor(id, dex)))
            .Where(item => item.Source is not null)
            .SelectMany(item => gameId == "home"
                ? [(item.GameId, Area: (string?)null, Pokemon: p, RaidMethod: (string?)null)]
                : item.Source!.Areas.Where(area => !string.IsNullOrWhiteSpace(area))
                    .Select(area => (item.GameId, Area: (string?)area.Trim(), Pokemon: p, RaidMethod: (string?)null))
                    .Concat(item.Source.Raids.Select(raid => (item.GameId, Area: (string?)raid.Detail, Pokemon: p, RaidMethod: (string?)raid.Method))))).ToList();
        var locations = located.GroupBy(item => (item.GameId, item.Area, item.RaidMethod))
            .Select(group => new RankedLocation(group.Key.GameId, group.Key.Area,
                group.Select(item => item.Pokemon).DistinctBy(p => p.Id).ToList()) { RaidMethod = group.Key.RaidMethod })
            .OrderByDescending(location => location.Pokemon.Count)
            .ThenBy(location => location.Area ?? location.GameId, StringComparer.Ordinal)
            .ThenBy(location => location.GameId, StringComparer.Ordinal).ToList();
        var locatedIds = located.Select(item => item.Pokemon.Id).ToHashSet();
        return new(locations.Where(location => location.RaidMethod is null).ToList(), remaining.Where(p => !locatedIds.Contains(p.Id)).ToList())
        { Raids = locations.Where(location => location.RaidMethod is not null).ToList() };
    }
}
