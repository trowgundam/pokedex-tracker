using System.Text.Json;
using PokedexTracker.Core;

if (args.Length is not (0 or 1 or 3) || (args.Length == 3 && args[1] != "--category"))
    throw new ArgumentException("Usage: Checks [repository root] [--category catalog|generation|tracker]");
string root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
string category = args.Length == 3 ? args[2] : "all";
string[] categories = ["catalog", "generation", "tracker"];
if (category != "all" && !categories.Contains(category))
    throw new ArgumentException($"Unknown check category: {category}");
Catalog catalog = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(Path.Combine(root, "pokedex_tracker/wwwroot/data/catalog.json")), TrackerJson.Options)!;
int total = 0;
foreach (string selected in category == "all" ? categories : [category])
{
    int checks = selected switch
    {
        "catalog" => CatalogChecks.Run(catalog, root),
        "generation" => AcquisitionChecks.RunFixtures() + EvolutionCoverageChecks.RunBuilder(),
        "tracker" => await TrackerChecks.Run(catalog),
        _ => throw new InvalidOperationException($"Unknown check category: {selected}")
    };
    total += checks;
    Console.WriteLine($"PASS: {checks} {selected} checks.");
}
if (category == "all") Console.WriteLine($"PASS: {total} catalog and tracker behavior checks.");
