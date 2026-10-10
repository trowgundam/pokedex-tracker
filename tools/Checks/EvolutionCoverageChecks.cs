using System.Text.Json;
using PokedexTracker.Core;
using PokedexTracker.CatalogGenerator;

static class EvolutionCoverageChecks
{
    public static int RunCatalog(Catalog catalog, string root)
    {
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        var report = JsonSerializer.Deserialize<EvolutionCoverageReport>(File.ReadAllText(Path.Combine(root, "tools/CatalogGenerator/evolution-coverage.json")), TrackerJson.Options)!;
        Check(report.Complete, "Every current candidate has a completed coverage decision.");
        Check(report.Paths.Select(p => (p.GameId, p.FromId, p.ToId)).Distinct().Count() == report.Paths.Count, "The inventory accounts for each candidate exactly once.");
        foreach (var path in report.Paths)
        {
            var edges = catalog.Evolutions[path.GameId].Where(e => e.FromId == path.FromId && e.ToId == path.ToId).ToList();
            Check(path.Sources.Count > 0 && path.Sources.All(s => Uri.TryCreate(s, UriKind.Absolute, out var uri) && uri.Scheme == "https") && path.Reason.Length > 0,
                "Every included or unavailable path has its explanation and source evidence.");
            Check(path.Status == EvolutionCoverageStatus.Included
                    ? edges.Count == 1 && edges[0].Requirement == string.Join(" or ", path.Requirements)
                    : edges.Count == 0 && path.Requirements.Count == 0,
                "The published tree matches the completed coverage decision.");
        }
        foreach (var pair in catalog.Evolutions)
            foreach (var edge in pair.Value)
                Check(report.Paths.Any(p => p.GameId == pair.Key && p.FromId == edge.FromId && p.ToId == edge.ToId && p.Status == EvolutionCoverageStatus.Included),
                    "Every published edge is accounted for by the inventory.");

        return checks;
    }

    public static int RunBuilder()
    {
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        // A small independent catalog lets these checks remove entire source rows,
        // invent a future condition, and add a future game without touching real data.
        var fixture = new Fixture();
        var baseline = fixture.Build();
        Check(baseline.Coverage.Complete && baseline.Evolutions["scarlet"].SequenceEqual([
            new Evolution("parent", "child", "Level 20"), new Evolution("other-parent", "other-child", "Level 30")]),
            "Generic source methods produce literal expected requirements.");

        var regionalFixture = new Fixture();
        regionalFixture.Catalog = regionalFixture.Catalog with { Pokemon = [..regionalFixture.Catalog.Pokemon,
            regionalFixture.Catalog.Pokemon[0] with { Id = "parent-galar", Form = "Galarian", SpriteId = 5 }] };
        var regionalPath = regionalFixture.Build().Coverage.Paths.Single(p => p.FromId == "parent-galar");
        Check(regionalPath.Status == EvolutionCoverageStatus.Unavailable && regionalPath.Rules.Single().Outcome == "Requires parent parent" &&
            regionalPath.RuleFingerprint == EvolutionRuleFingerprint.Of([regionalFixture.Rules[0]]),
            "A cross-form exclusion records the actual source parent and fingerprints that evidence.");
        regionalFixture.Rules.RemoveAt(0);
        Check(regionalFixture.Build().Coverage.Paths.Where(p => p.ToId == "child").All(p => p.Status == EvolutionCoverageStatus.Unresolved),
            "Removing the source form evidence reopens all its candidate parents for research.");

        fixture.Rules.Clear();
        var missing = fixture.Build();
        Check(!missing.Coverage.Complete && missing.Coverage.Paths.Count == 2 && missing.Coverage.Paths.All(p => p.Status == EvolutionCoverageStatus.Unresolved && p.Rules.Count == 0),
            "Missing whole source rows remain in the family inventory for research.");

        fixture = new();
        foreach (var rule in fixture.Rules) rule["version_group_id"] = "999";
        var filtered = fixture.Build();
        Check(filtered.Coverage.Paths.Count == 2 && filtered.Coverage.Paths.All(p => p.Status == EvolutionCoverageStatus.Unresolved && p.Rules.Single().Outcome == "Version group is outside the inheritance policy"),
            "Version filters cannot make candidate families disappear.");

        fixture = new();
        foreach (var rule in fixture.Rules) rule["evolution_trigger_id"] = "999";
        var unknown = fixture.Build();
        Check(unknown.Coverage.Paths.Count(p => p.Status == EvolutionCoverageStatus.Unresolved) == 2 && unknown.Evolutions["scarlet"].Count == 0,
            "Unknown mechanics collect the whole research worklist instead of stopping at the first one.");

        fixture = new();
        fixture.Rules[0]["minimum_jumps"] = "3";
        var newCondition = fixture.Build();
        Check(newCondition.Coverage.Paths.Single(p => p.ToId == "child").Status == EvolutionCoverageStatus.Unresolved &&
            newCondition.Coverage.Paths.Single(p => p.ToId == "child").Reason.Contains("minimum_jumps=3"),
            "A new upstream condition is named in the worklist and cannot be silently shortened.");

        fixture = new();
        fixture.Rules[0]["minimum_damage_taken"] = "49";
        Check(fixture.Build().Coverage.Paths.Single(p => p.ToId == "child").Status == EvolutionCoverageStatus.Unresolved,
            "A known condition on an unsupported trigger also requires implementation.");

        fixture = new();
        fixture.Rules.Add(new(fixture.Rules[0]) { ["id"] = "3", ["version_group_id"] = "27", ["evolved_pokemon_form_id"] = "999" });
        Check(fixture.Build().Coverage.Issues.Any(i => i.Contains("source rule 3 uses an unresolved parent or result form")),
            "An unresolved new source form remains in the worklist even when an older ordinary method exists.");

        fixture = new();
        var decision = new EvolutionDecision("Scarlet / Violet", "parent", "child", "Use the verified special method",
            "Reviewed method", "https://example.org/evolution", EvolutionRuleFingerprint.Of([fixture.Rules[0]]));
        var supplemented = fixture.Build([decision]);
        Check(supplemented.Coverage.Complete && supplemented.Evolutions["scarlet"].First().Requirement == "Use the verified special method",
            "A cited supplement completes a path through the same generator.");
        fixture.Rules[0]["minimum_level"] = "21";
        var changed = fixture.Build([decision]);
        Check(changed.Coverage.Paths.Single(p => p.ToId == "child").Status == EvolutionCoverageStatus.Unresolved &&
            changed.Coverage.Paths.Single(p => p.ToId == "child").Reason == "Source rules changed. Research and update this reviewed decision.",
            "Changed upstream evidence reopens a supplement for review.");

        fixture = new();
        var exclusion = decision with { Requirement = null, Reason = "The required item is unavailable locally", RuleFingerprint = EvolutionRuleFingerprint.Of([fixture.Rules[0]]) };
        var unavailable = fixture.Build([exclusion]);
        Check(unavailable.Coverage.Complete && unavailable.Coverage.Paths.Single(p => p.ToId == "child").Status == EvolutionCoverageStatus.Unavailable &&
            unavailable.Evolutions["scarlet"].All(e => e.ToId != "child"), "A cited local restriction is accounted for without inventing an evolution.");
        fixture.Rules.Add(new(fixture.Rules[0]) { ["id"] = "3", ["minimum_level"] = "22" });
        Check(fixture.Build([exclusion]).Coverage.Paths.Single(p => p.ToId == "child").Status == EvolutionCoverageStatus.Unresolved,
            "A newly added upstream method reopens an exclusion instead of inheriting it blindly.");

        fixture = new();
        fixture.Catalog = fixture.Catalog with { Games = [new("future", "Future", "Future games")], Pokemon = fixture.Catalog.Pokemon.Select(p => p with { Sources = new() { ["future"] = [] } }).ToList() };
        var future = fixture.Build();
        Check(future.Coverage.Paths.Count == 2 && future.Coverage.Paths.All(p => p.Status == EvolutionCoverageStatus.Unresolved) && future.Coverage.Issues.Single().Contains("reviewed game policy"),
            "A future game inventories every family and requests its own policy research.");

        fixture = new();
        fixture.Rules.Add(new(fixture.Rules[0]) { ["id"] = "3", ["version_group_id"] = "27", ["evolution_trigger_id"] = "999" });
        Check(fixture.Build().Coverage.Paths.Single(p => p.ToId == "child").Status == EvolutionCoverageStatus.Unresolved,
            "An unsupported current override never falls back to an older method.");
        Check(fixture.Build([decision, decision]).Coverage.Issues.Any(i => i.Contains("duplicate decision")), "Conflicting decisions remain visible as coverage issues.");
        return checks;
    }

    private sealed class Fixture
    {
        public Catalog Catalog { get; set; } = new()
        {
            Version = "fixture", Games = [new("scarlet", "Scarlet", "Scarlet / Violet")], Dexes = [],
            Pokemon = Enumerable.Range(1, 4).Select(id => new PokemonVariant
            {
                Id = new[] { "parent", "child", "other-parent", "other-child" }[id - 1], Name = "Fixture " + id,
                NationalNumber = id, SpriteId = id, Sources = new() { ["scarlet"] = [] }
            }).ToList()
        };
        public List<Dictionary<string, string>> Rules { get; } = [Rule("1", "2", "20"), Rule("2", "4", "30")];
        public EvolutionBuildResult Build(IReadOnlyList<EvolutionDecision>? decisions = null) =>
            EvolutionBuilder.Build(Catalog, Csv, decisions ?? [], "https://example.org/source.csv");
        private List<Dictionary<string, string>> Csv(string name) => name switch
        {
            "pokemon_evolution" => Rules,
            "pokemon_species" => Enumerable.Range(1, 4).Select(id => new Dictionary<string, string>
                { ["id"] = id.ToString(), ["evolves_from_species_id"] = id % 2 == 0 ? (id - 1).ToString() : "" }).ToList(),
            "pokemon" => Catalog.Pokemon.Select(p => new Dictionary<string, string>
                { ["id"] = p.SpriteId.ToString(), ["species_id"] = p.NationalNumber.ToString(), ["identifier"] = p.Id }).ToList(),
            "pokemon_forms" or "item_names" or "move_names" or "type_names" => [],
            _ => throw new InvalidOperationException("Unexpected fixture table: " + name)
        };
        private static Dictionary<string, string> Rule(string id, string target, string level)
        {
            const string fields = "id evolved_species_id evolution_trigger_id version_group_id is_default trigger_item_id minimum_level gender_id location_id held_item_id time_of_day known_move_id known_move_type_id minimum_happiness minimum_beauty minimum_affection relative_physical_stats party_species_id party_type_id trade_species_id needs_overworld_rain turn_upside_down needs_multiplayer near_special_rock region_id required_pokemon_form_id evolved_pokemon_form_id used_move_id minimum_move_count minimum_steps minimum_damage_taken nature_bitmask condition_expression percentage_chance";
            var row = fields.Split(' ').ToDictionary(k => k, _ => "");
            row["id"] = id; row["evolved_species_id"] = target; row["evolution_trigger_id"] = "1";
            row["version_group_id"] = "25"; row["is_default"] = "1"; row["minimum_level"] = level;
            return row;
        }
    }
}
