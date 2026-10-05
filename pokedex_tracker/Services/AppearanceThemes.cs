namespace PokedexTracker.Services;

public sealed record AppearanceTheme(string Id, string Name);

public static class AppearanceThemes
{
    // Each registered ID has a matching selector in wwwroot/css/themes.css.
    public static IReadOnlyList<AppearanceTheme> All { get; } =
    [
        new("catppuccin", "Catppuccin"),
        new("game", "Game Specific")
    ];

    public static string Normalize(string? id) => All.FirstOrDefault(theme => theme.Id == id)?.Id ?? All[0].Id;
}
