using PokedexTracker.Core;

static class EvolutionChecks
{
    public static int Run(Catalog catalog)
    {
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        List<List<Evolution>> Paths(string game, string id) => EvolutionTree.Paths(catalog, game, id);
        Check(Paths("scarlet", "floragato").Single().SequenceEqual([
            new Evolution("sprigatito", "floragato", "Level 16"),
            new Evolution("floragato", "meowscarada", "Level 36")]), "Selecting the middle stage shows the entire starter family with exact levels.");
        Check(Paths("scarlet", "eevee").Select(p => p.Last().ToId).Order().SequenceEqual([
            "espeon", "flareon", "glaceon", "jolteon", "leafeon", "sylveon", "umbreon", "vaporeon"]), "Eevee shows all eight verified branches in Scarlet.");
        Check(Paths("scarlet", "sylveon").Single(p => p.Last().ToId == "sylveon").Last().Requirement == "Level up · with high friendship · knowing a Fairy-type move", "Sylveon's requirement includes both friendship and its move.");
        Check(Paths("scarlet", "perrserker").Single().SequenceEqual([new Evolution("meowth-galar", "perrserker", "Level 28")]), "Regional families do not inherit ordinary Meowth or Persian.");
        Check(Paths("scarlet", "meowth").Single().SequenceEqual([new Evolution("meowth", "persian", "Level 28")]), "Ordinary Meowth has its own family.");
        Check(Paths("legends-arceus", "quilava").Single().SequenceEqual([
            new Evolution("cyndaquil", "quilava", "Evolve at level 17"),
            new Evolution("quilava", "typhlosion-hisui", "Evolve at level 36")]), "Hisui overrides Cyndaquil's level and Typhlosion's form.");
        Check(Paths("scarlet", "quilava").Single().First().Requirement == "Level 14", "Scarlet does not inherit Hisui's starter override.");
        string Requirement(string game, string from, string to) => catalog.Evolutions[game].Single(e => e.FromId == from && e.ToId == to).Requirement;
        Check(Requirement("scarlet", "primeape", "annihilape") == "Use Rage Fist 20 times · then level up", "Primeape's complete Scarlet requirement is present.");
        Check(Requirement("legends-za", "primeape", "annihilape") == "Use Rage Fist 20 times · then evolve", "Z-A uses manual evolution after Rage Fist.");
        Check(Requirement("legends-arceus", "qwilfish-hisui", "overqwil") == "Use Barb Barrage 20 times in Strong Style · then evolve" &&
            Requirement("legends-za", "qwilfish-hisui", "overqwil") == "Use Barb Barrage 20 times · then evolve" &&
            Requirement("scarlet", "qwilfish-hisui", "overqwil") == "Level up · knowing Barb Barrage", "Overqwil retains the three different game requirements.");
        Check(Requirement("brilliant-diamond", "eevee", "leafeon") == "Level up near the Moss Rock in Eterna Forest" &&
            Requirement("sword", "eevee", "leafeon") == "Use Leaf Stone", "BDSP retains its location requirement.");
        Check(Requirement("legends-arceus", "haunter", "gengar") == "Use Linking Cord or trade" &&
            Requirement("scarlet", "haunter", "gengar") == "Trade", "Arceus includes the Linking Cord alternative.");
        Check(Requirement("legends-arceus", "sneasel-hisui", "sneasler") == "Use Razor Claw · during the day", "Arceus uses evolution items directly.");
        Check(Requirement("scarlet", "pawmo", "pawmot") == "Level up · after walking 1000 steps outside its Poké Ball in one go using Let's Go", "Walking requirements retain the distance and mode.");
        Check(Requirement("sword", "karrablast", "escavalier") == "Trade · for Shelmet", "Partner trades name the required partner.");
        Check(Paths("scarlet", "kleavor").Count == 0 && Paths("scarlet", "ursaluna").Count == 0 && Paths("legends-za", "kleavor").Count == 0,
            "Native encounters do not imply that unavailable Hisui evolution items work in another game.");
        Check(Requirement("scarlet", "growlithe-hisui", "arcanine-hisui") == "Use Fire Stone" &&
            Requirement("scarlet", "basculin-white-striped", "basculegion") == "Take 294 recoil damage without fainting · then level up · gender determines form" &&
            Requirement("legends-za", "sliggoo-hisui", "goodra-hisui") == "Evolve at level 50 · in overworld rain", "Regional parents retain portable methods with the local evolution action.");
        Check(Requirement("scarlet", "kubfu", "urshifu").Contains("Use Scroll of Darkness") && Requirement("scarlet", "kubfu", "urshifu").Contains("Use Scroll of Waters"),
            "Both permanent Urshifu forms retain their item alternatives in one identity.");
        Check(Requirement("scarlet", "sinistea", "polteageist").Contains("Use Cracked Pot · for its Phony Form") &&
            Requirement("scarlet", "sinistea", "polteageist").Contains("Use Chipped Pot · for its Antique Form"), "Both Polteageist methods retain their required parent forms.");
        Check(Requirement("scarlet", "rockruff", "lycanroc").Contains("with Own Tempo") && Requirement("scarlet", "rockruff", "lycanroc").Contains("into its Dusk Form"),
            "Collapsing cosmetic forms preserves Lycanroc's ability and form conditions.");
        // Literal coverage expectations from the missing-path audit, independent of generated rows.
        foreach (var (game, targets) in new[]
        {
            ("sword", "sirfetchd escavalier urshifu mantine alcremie shedinja pangoro accelgor toxtricity runerigus"),
            ("brilliant-diamond", "glaceon leafeon magnezone mantine shedinja probopass cascoon silcoon"),
            ("legends-arceus", "basculegion dusknoir glaceon leafeon sylveon electivire gliscor golem chansey gengar alakazam machamp magmortar magnezone probopass mantine steelix porygon2 porygon-z overqwil rhyperior scizor weavile sneasler wyrdeer cascoon silcoon"),
            ("scarlet", "kingambit brambleghast dudunsparce gholdengo alcremie pawmot annihilape rabsca maushold toxtricity arcanine-hisui basculegion"),
            ("legends-za", "sylveon sirfetchd milotic gholdengo gengar malamar alakazam machamp steelix pangoro trevenant porygon2 porygon-z annihilape gourgeist overqwil scizor goodra slowking aromatisse slurpuff toxtricity runerigus goodra-hisui")
        })
            foreach (string target in targets.Split(' '))
                Check(Paths(game, target).Any(path => path.Any(edge => edge.ToId == target)), $"{game} contains audited evolution into {target}.");
        Check(Paths("scarlet", "ditto").Count == 0 && Paths("home", "eevee").Count == 0, "Pokémon without verified evolutions and non-game HOME have no tree.");
        Check(Paths("firered", "crobat").Single().Last().Requirement == "Level up · with high friendship · after obtaining the National Pokédex", "FireRed includes the National Pokédex restriction for later-generation evolutions.");
        var ids = catalog.Pokemon.Select(p => p.Id).ToHashSet();
        foreach (var pair in catalog.Evolutions)
        {
            Check(catalog.Games.Any(g => g.Id == pair.Key), "Evolution rules belong to a known game.");
            foreach (var edge in pair.Value)
                Check(ids.Contains(edge.FromId) && ids.Contains(edge.ToId) && edge.FromId != edge.ToId && edge.Requirement.Length > 0,
                    "Every evolution has distinct catalog identities and a requirement.");
            foreach (string id in pair.Value.Select(e => e.FromId).Distinct())
                Check(Paths(pair.Key, id).Count > 0, "Every generated family can be traversed.");
        }
        return checks;
    }
}
