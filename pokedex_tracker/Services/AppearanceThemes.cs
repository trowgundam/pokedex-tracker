namespace PokedexTracker.Services;

public sealed record AppearanceAccent(string Id, string Name);
public sealed record AppearanceTheme(string Id, string Name, IReadOnlyList<AppearanceAccent> Accents, string? AccentDescription = null);

public static class AppearanceThemes
{
    // Each registered theme and accent has a matching selector in wwwroot/css/themes.css.
    // The first accent is the default. A single accent needs no selector.
    public static IReadOnlyList<AppearanceTheme> All { get; } =
    [
        new("catppuccin", "Catppuccin",
        [
            new("blue", "Blue"),
            new("rosewater", "Rosewater"),
            new("flamingo", "Flamingo"),
            new("pink", "Pink"),
            new("mauve", "Mauve"),
            new("red", "Red"),
            new("maroon", "Maroon"),
            new("peach", "Peach"),
            new("yellow", "Yellow"),
            new("green", "Green"),
            new("teal", "Teal"),
            new("sky", "Sky"),
            new("sapphire", "Sapphire"),
            new("lavender", "Lavender")
        ]),
        new("normal", "Normal", [new("game", "Current game")],
            "Accent color follows the current game. The welcome screen and National Pokédex use blue.")
    ];

    public static AppearanceTheme Find(string? id) => All.FirstOrDefault(theme => theme.Id == (id == "game" ? "normal" : id)) ?? All[0];
    public static string Normalize(string? id) => Find(id).Id;
    public static string NormalizeAccent(string themeId, string? accentId)
    {
        var theme = Find(themeId);
        return theme.Accents.FirstOrDefault(accent => accent.Id == accentId)?.Id ?? theme.Accents[0].Id;
    }
}
