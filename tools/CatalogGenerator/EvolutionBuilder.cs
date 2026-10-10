using System.Globalization;
using PokedexTracker.Core;

namespace PokedexTracker.CatalogGenerator;

public static class EvolutionBuilder
{
    // Inventory comes from species relationships, independently of evolution-row filtering.
    // The returned worklist contains every unresolved path; callers resolve it before writing the catalog.
    public static EvolutionBuildResult Build(Catalog catalog, Func<string, List<Dictionary<string, string>>> csv,
        IReadOnlyList<EvolutionDecision> decisions, string ruleSource)
    {
        var species = csv("pokemon_species").ToDictionary(r => r["id"]);
        var pokemon = csv("pokemon").ToDictionary(r => r["id"]);
        var forms = csv("pokemon_forms").ToDictionary(r => r["id"]);
        var variants = catalog.Pokemon.ToDictionary(p => p.Id);
        var bySprite = catalog.Pokemon.ToDictionary(p => p.SpriteId.ToString());
        var defaults = catalog.Pokemon.Where(p => p.Form.Length == 0).ToDictionary(p => p.NationalNumber.ToString());
        var items = English("item_names", "item_id");
        var moves = English("move_names", "move_id");
        var types = English("type_names", "type_id");
        var rules = csv("pokemon_evolution");
        var knownColumns = "id evolved_species_id evolution_trigger_id version_group_id is_default trigger_item_id minimum_level gender_id location_id held_item_id time_of_day known_move_id known_move_type_id minimum_happiness minimum_beauty minimum_affection relative_physical_stats party_species_id party_type_id trade_species_id needs_overworld_rain turn_upside_down needs_multiplayer near_special_rock region_id required_pokemon_form_id evolved_pokemon_form_id used_move_id minimum_move_count minimum_steps minimum_damage_taken nature_bitmask condition_expression percentage_chance".Split(' ').ToHashSet();
        List<EvolutionCoveragePath> coverage = [];
        List<string> issues = [];
        Dictionary<string, List<Evolution>> result = [];
        var decisionGroups = decisions.GroupBy(d => (d.GameGroup, d.FromId, d.ToId)).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var group in decisionGroups.Values)
        {
            var d = group[0];
            if (group.Count != 1 || d.Reason.Length == 0 || !Uri.TryCreate(d.Source, UriKind.Absolute, out var uri) || uri.Scheme != "https" || d.Requirement == "")
                issues.Add($"Invalid or duplicate decision: {d.GameGroup}: {d.FromId} -> {d.ToId}");
            if (!catalog.Games.Any(g => g.Group == d.GameGroup) || !variants.ContainsKey(d.FromId) || !variants.ContainsKey(d.ToId))
                issues.Add($"Decision references an unknown game or identity: {d.GameGroup}: {d.FromId} -> {d.ToId}");
        }
        var usedDecisions = new HashSet<EvolutionDecision>();
        var mappedRules = rules.Select(r =>
        {
            var target = Resolve(r["evolved_pokemon_form_id"], r["evolved_species_id"]);
            string parentSpecies = species[r["evolved_species_id"]]["evolves_from_species_id"];
            var parent = parentSpecies.Length == 0 ? null : Resolve(r["required_pokemon_form_id"], parentSpecies);
            if (parent is not null && target is not null && r["required_pokemon_form_id"].Length == 0 && target.Form.Length > 0)
                parent = catalog.Pokemon.SingleOrDefault(p => p.NationalNumber.ToString() == parentSpecies && p.Form == target.Form) ?? parent;
            return new ResolvedRule(parent, target, r);
        }).ToList();
        var resolved = mappedRules.Where(r => r.Parent is not null && r.Target is not null).ToLookup(r => r.Target!.Id);
        foreach (var game in catalog.Games.Where(g => g.Id != "home"))
        {
            var policy = Policy(game.Group);
            if (policy is null) issues.Add($"Research evolution rules and add a reviewed game policy for {game.Id} ({game.Group}).");
            result[game.Id] = [];
            var native = catalog.Pokemon.Where(p => p.Sources.ContainsKey(game.Id)).ToLookup(p => p.NationalNumber.ToString());
            if (policy is not null)
                foreach (var rule in mappedRules.Where(r => r.Parent is null || r.Target is null))
                {
                    string parentSpecies = species[rule.Data["evolved_species_id"]]["evolves_from_species_id"];
                    int version = int.Parse(rule.Data["version_group_id"]);
                    if (parentSpecies.Length > 0 && native[rule.Data["evolved_species_id"]].Any() && native[parentSpecies].Any() &&
                        policy.Versions.Contains(version) && (rule.Data["is_default"] == "1" || policy.Current.Contains(version)) &&
                        (rule.Data["region_id"].Length == 0 || rule.Data["region_id"] == policy.Region))
                        issues.Add($"{game.Id}: source rule {rule.Data["id"]} uses an unresolved parent or result form. Research its identity and local availability.");
                }
            foreach (var target in native.SelectMany(g => g).OrderBy(p => p.Id, StringComparer.Ordinal))
            {
                string parentSpecies = species[target.NationalNumber.ToString()]["evolves_from_species_id"];
                foreach (var parent in native[parentSpecies].OrderBy(p => p.Id, StringComparer.Ordinal))
                {
                    var related = resolved[target.Id].ToList();
                    var edgeRules = related.Where(r => r.Parent!.Id == parent.Id).OrderBy(r => int.Parse(r.Data["id"])).ToList();
                    var eligible = edgeRules.Where(r => FilterReason(r, policy) is null).ToList();
                    int priority = eligible.Count == 0 ? 0 : eligible.Max(r => Priority(r, policy!));
                    var selected = eligible.Where(r => Priority(r, policy!) == priority).ToList();
                    List<EvolutionRuleTrace> trace = edgeRules.Select(r => new EvolutionRuleTrace(r.Data["id"],
                        FilterReason(r, policy) ?? (selected.Contains(r) ? "Selected" : "Superseded by a newer game rule"))).ToList();
                    List<string> sources = policy is null ? [ruleSource] : [ruleSource, policy.Source];
                    var decision = decisionGroups.GetValueOrDefault((game.Group, parent.Id, target.Id))?.FirstOrDefault();
                    if (decision is not null) usedDecisions.Add(decision);
                    EvolutionCoverageStatus status = EvolutionCoverageStatus.Unresolved;
                    string reason;
                    List<string> requirements = [];
                    if (policy is null) reason = "Game evolution policy needs research.";
                    else if (decision is not null)
                    {
                        sources.Add(decision.Source);
                        // Exclusions review all source rows, so newly added methods reopen the decision.
                        var evidence = edgeRules;
                        if (decision.RuleFingerprint != EvolutionRuleFingerprint.Of(evidence.Select(r => r.Data)))
                            reason = "Source rules changed. Research and update this reviewed decision.";
                        else
                        {
                            status = decision.Requirement is null ? EvolutionCoverageStatus.Unavailable : EvolutionCoverageStatus.Included;
                            reason = decision.Reason;
                            if (decision.Requirement is not null) requirements.Add(decision.Requirement);
                        }
                    }
                    else if (edgeRules.Count == 0 && related.Count > 0)
                    {
                        status = EvolutionCoverageStatus.Unavailable;
                        reason = "Source rules for this target identity use other parent forms.";
                        trace = related.Select(r => new EvolutionRuleTrace(r.Data["id"], "Requires parent " + r.Parent!.Id)).ToList();
                    }
                    else if (selected.Count == 0)
                        reason = "No applicable source method. Research whether this path evolves locally and add its method or a cited explanation.";
                    else
                    {
                        var descriptions = selected.Select(r => HasUnknownColumns(r.Data) ? null : Describe(r.Data, game.Id)).ToList();
                        if (descriptions.Any(d => d is null))
                            reason = "Research and implement source conditions: " + string.Join("; ", selected.Select(r =>
                                "rule " + r.Data["id"] + " [" + string.Join(", ", r.Data.Where(p => p.Value.Length > 0 && p.Value != "0" &&
                                    p.Key is not ("id" or "evolved_species_id" or "version_group_id" or "is_default" or "required_pokemon_form_id" or "evolved_pokemon_form_id"))
                                    .Select(p => p.Key + "=" + p.Value)) + "]"));
                        else
                        {
                            status = EvolutionCoverageStatus.Included;
                            reason = "Requirements described from applicable source rules.";
                            requirements = descriptions.Select(d => d!).Distinct().Order(StringComparer.Ordinal).ToList();
                        }
                    }
                    coverage.Add(new(game.Id, parent.Id, target.Id, status, requirements, reason, sources.Distinct().ToList(), trace, EvolutionRuleFingerprint.Of((edgeRules.Count == 0 && related.Count > 0 ? related : edgeRules).Select(r => r.Data))));
                    if (status == EvolutionCoverageStatus.Included)
                        result[game.Id].Add(new(parent.Id, target.Id, string.Join(" or ", requirements)));
                }
            }
            result[game.Id] = result[game.Id].OrderBy(e => variants[e.FromId].NationalNumber).ThenBy(e => e.FromId).ThenBy(e => e.ToId).ToList();
        }
        foreach (var decision in decisions.Except(usedDecisions))
            issues.Add($"Decision no longer matches a native family: {decision.GameGroup}: {decision.FromId} -> {decision.ToId}");
        return new(result, new(ruleSource, coverage, issues));

        bool HasUnknownColumns(Dictionary<string, string> r) => r.Keys.Any(k => !knownColumns.Contains(k));
        Dictionary<string, string> English(string table, string key) => csv(table).Where(r => r["local_language_id"] == "9").ToDictionary(r => r[key], r => r["name"]);
        PokemonVariant? Resolve(string form, string speciesId)
        {
            if (form.Length == 0) return defaults.GetValueOrDefault(speciesId);
            if (!forms.TryGetValue(form, out var row) || !pokemon.TryGetValue(row["pokemon_id"], out var entry)) return null;
            if (bySprite.TryGetValue(row["pokemon_id"], out var variant)) return variant;
            // Permanent cosmetic forms share an ordinary checklist identity. Regional
            // identities must be represented explicitly and never collapse to ordinary.
            string identifier = entry["identifier"];
            return new[] { "-alola", "-galar", "-hisui", "-paldea", "-white-striped" }.Any(identifier.Contains)
                ? null : defaults.GetValueOrDefault(entry["species_id"]);
        }
        string? Describe(Dictionary<string, string> r, string gameId)
        {
            // Unknown mechanics are reported for research and implementation.
            if (r["location_id"].Length > 0 || r["minimum_affection"].Length > 0 || r["condition_expression"].Length > 0 || r["nature_bitmask"].Length > 0 || r["percentage_chance"].Length > 0) return null;
            if (new[] { "held_item_id", "trigger_item_id" }.Any(k => r[k].Length > 0 && !items.ContainsKey(r[k])) ||
                new[] { "used_move_id", "known_move_id" }.Any(k => r[k].Length > 0 && !moves.ContainsKey(r[k])) ||
                new[] { "known_move_type_id", "party_type_id" }.Any(k => r[k].Length > 0 && !types.ContainsKey(r[k])) ||
                new[] { "party_species_id", "trade_species_id" }.Any(k => r[k].Length > 0 && !defaults.ContainsKey(r[k]))) return null;
            if (r["gender_id"].Length > 0 && r["gender_id"] is not ("1" or "2")) return null;
            if (r["relative_physical_stats"].Length > 0 && r["relative_physical_stats"] is not ("0" or "1" or "-1")) return null;
            string? action = r["evolution_trigger_id"] switch
            {
                "1" => r["minimum_level"].Length > 0 ? $"Level {r["minimum_level"]}" : "Level up",
                "2" => "Trade",
                "3" when items.TryGetValue(r["trigger_item_id"], out string? item) => "Use " + item,
                "11" or "12" => $"Use {moves[r["used_move_id"]]} {r["minimum_move_count"]} times in {(r["evolution_trigger_id"] == "11" ? "Agile" : "Strong")} Style · then evolve",
                "14" => $"Use {moves[r["used_move_id"]]} {r["minimum_move_count"]} times · then {(gameId == "legends-za" ? "evolve" : "level up")}",
                _ => null
            };
            if (action is null) return null;
            if (r["used_move_id"].Length > 0 && r["evolution_trigger_id"] is not ("11" or "12" or "14")) return null;
            if (r["minimum_move_count"].Length > 0 && r["evolution_trigger_id"] is not ("11" or "12" or "14")) return null;
            if (r["minimum_damage_taken"].Length > 0 || r["near_special_rock"] == "1") return null;
            if (r["trigger_item_id"].Length > 0 && r["evolution_trigger_id"] != "3") return null;
            if (r["time_of_day"].Length > 0 && r["time_of_day"] is not ("day" or "night" or "dusk" or "full-moon")) return null;
            if (gameId == "legends-arceus" && r["held_item_id"].Length > 0)
                action = "Use " + items[r["held_item_id"]];
            List<string> parts = [action];
            if (r["minimum_steps"].Length > 0) parts.Add($"after walking {r["minimum_steps"]} steps outside its Poké Ball in one go using Let's Go");
            if (r["party_species_id"].Length > 0) parts.Add("with " + defaults[r["party_species_id"]].Name + " in the party");
            if (r["party_type_id"].Length > 0) parts.Add("with a " + types[r["party_type_id"]] + "-type Pokémon in the party");
            if (r["trade_species_id"].Length > 0) parts.Add("for " + defaults[r["trade_species_id"]].Name);
            if (r["minimum_beauty"].Length > 0) parts.Add("with Beauty at least " + r["minimum_beauty"]);
            if (r["required_pokemon_form_id"].Length > 0 && forms.TryGetValue(r["required_pokemon_form_id"], out var parentForm) &&
                Resolve(r["required_pokemon_form_id"], "")?.Form.Length == 0 && parentForm["form_identifier"].Length > 0)
            {
                string formName = parentForm["form_identifier"];
                parts.Add(formName == "own-tempo" ? "with Own Tempo" : "for its " + CultureInfo.InvariantCulture.TextInfo.ToTitleCase(formName.Replace('-', ' ')) + " Form");
            }
            if (r["evolved_pokemon_form_id"].Length > 0 && forms.TryGetValue(r["evolved_pokemon_form_id"], out var targetForm) &&
                Resolve(r["evolved_pokemon_form_id"], "")?.Form.Length == 0 && targetForm["form_identifier"].Length > 0 &&
                targetForm["form_identifier"] != parentFormName())
                parts.Add("into its " + CultureInfo.InvariantCulture.TextInfo.ToTitleCase(targetForm["form_identifier"].Replace('-', ' ')) + " Form");
            string parentFormName() => forms.TryGetValue(r["required_pokemon_form_id"], out var parentRow) ? parentRow["form_identifier"] : "";
            if (r["held_item_id"].Length > 0 && gameId != "legends-arceus") parts.Add("holding " + items[r["held_item_id"]]);
            if (r["minimum_happiness"].Length > 0) parts.Add("with high friendship");
            if (r["known_move_id"].Length > 0) parts.Add("knowing " + moves[r["known_move_id"]]);
            if (r["known_move_type_id"].Length > 0) parts.Add("knowing a " + types[r["known_move_type_id"]] + "-type move");
            if (r["time_of_day"].Length > 0) parts.Add(r["time_of_day"] switch { "day" => "during the day", "night" => "at night", "dusk" => "at dusk", "full-moon" => "during a full moon", _ => throw new InvalidDataException("Unknown evolution time.") });
            if (r["evolved_species_id"] is "196" or "197" && gameId is "scarlet" or "violet" or "sword" or "shield" or "legends-arceus" or "legends-za") parts.Add("without a Fairy-type move");
            if (r["gender_id"].Length > 0) parts.Add(r["gender_id"] == "1" ? "female" : "male");
            if (r["relative_physical_stats"].Length > 0) parts.Add(r["relative_physical_stats"] switch { "1" => "Attack > Defense", "-1" => "Attack < Defense", "0" => "Attack = Defense", _ => throw new InvalidDataException("Unknown stat condition.") });
            if (r["needs_overworld_rain"] == "1") parts.Add("in overworld rain");
            if (r["turn_upside_down"] == "1") parts.Add("hold the system upside down");
            if (r["needs_multiplayer"] == "1") parts.Add("with another player in Union Circle");
            if (gameId is "firered" or "leafgreen" && int.Parse(r["evolved_species_id"]) > 151) parts.Add("after obtaining the National Pokédex");
            if (gameId is "legends-arceus" or "legends-za" && action.StartsWith("Level", StringComparison.Ordinal)) parts[0] = action == "Level up" ? "Evolve" : "Evolve at " + action.ToLowerInvariant();
            return string.Join(" · ", parts);
        }
    }

    private sealed record ResolvedRule(PokemonVariant? Parent, PokemonVariant? Target, Dictionary<string, string> Data);
    private sealed record GamePolicy(int[] Versions, int[] Current, string Region, string Source);
    private static GamePolicy? Policy(string group) => group switch
    {
        "FireRed / LeafGreen" => new([1, 3, 5, 7], [7], "1", "https://www.serebii.net/fireredleafgreen/"),
        "Let's Go" => new([1, 3, 5, 7, 8, 11, 15, 16, 17, 18, 19], [19], "1", "https://www.serebii.net/letsgopikachueevee/"),
        "Sword / Shield" => new([1, 3, 5, 7, 8, 11, 15, 16, 17, 18, 20, 21, 22], [20, 21, 22], "8", "https://www.serebii.net/swordshield/evolution.shtml"),
        "Brilliant Diamond / Shining Pearl" => new([1, 3, 5, 7, 8, 11, 15, 16, 17, 18, 20, 23], [8, 23], "4", "https://www.serebii.net/brilliantdiamondshiningpearl/evolution.shtml"),
        "Legends: Arceus" => new([1, 3, 5, 7, 8, 11, 15, 16, 17, 18, 20, 24], [24], "9", "https://www.serebii.net/legendsarceus/evolution.shtml"),
        "Scarlet / Violet" => new([1, 3, 5, 7, 8, 11, 15, 16, 17, 18, 20, 21, 22, 24, 25, 26, 27], [25, 26, 27], "10", "https://www.serebii.net/scarletviolet/evolution.shtml"),
        "Legends: Z-A" => new([1, 3, 5, 7, 8, 11, 15, 16, 17, 18, 20, 21, 22, 24, 25, 26, 27, 30], [30], "6", "https://www.serebii.net/legendsz-a/evolution.shtml"),
        _ => null
    };
    private static int Priority(ResolvedRule r, GamePolicy policy)
    {
        int version = int.Parse(r.Data["version_group_id"]);
        return policy.Current.Contains(version) ? 100 + version : version;
    }
    private static string? FilterReason(ResolvedRule r, GamePolicy? policy)
    {
        if (policy is null) return "Game policy needs research";
        int version = int.Parse(r.Data["version_group_id"]);
        if (!policy.Versions.Contains(version)) return "Version group is outside the inheritance policy";
        if (r.Data["is_default"] != "1" && !policy.Current.Contains(version)) return "Nondefault rule for another game";
        if (version == 24 && policy.Region != "9" && r.Parent!.Form.Length == 0) return "Hisui-specific method of an ordinary parent";
        if (r.Data["region_id"].Length > 0 && r.Data["region_id"] != policy.Region && r.Parent!.Form.Length == 0) return "Requires another region";
        return null;
    }
}
