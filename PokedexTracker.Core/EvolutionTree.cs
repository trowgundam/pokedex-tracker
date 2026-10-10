namespace PokedexTracker.Core;

public static class EvolutionTree
{
    public static List<List<Evolution>> Paths(Catalog catalog, string gameId, string pokemonId)
    {
        if (!catalog.Evolutions.TryGetValue(gameId, out var edges)) return [];
        HashSet<string> family = [pokemonId];
        bool changed;
        do
        {
            changed = false;
            foreach (var edge in edges.Where(e => family.Contains(e.FromId) || family.Contains(e.ToId)))
                changed |= family.Add(edge.FromId) | family.Add(edge.ToId);
        } while (changed);
        var members = edges.Where(e => family.Contains(e.FromId)).ToList();
        List<List<Evolution>> paths = [];
        foreach (string root in members.Select(e => e.FromId).Distinct().Where(id => !members.Any(e => e.ToId == id)))
            Visit(root, []);
        return paths;

        void Visit(string id, List<Evolution> path)
        {
            var children = members.Where(e => e.FromId == id).ToList();
            if (children.Count == 0 && path.Count > 0) paths.Add(path);
            foreach (var child in children) Visit(child.ToId, [.. path, child]);
        }
    }
}
