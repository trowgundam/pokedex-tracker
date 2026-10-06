using PokedexTracker.CatalogGenerator;
using PokedexTracker.Core;

internal static class AcquisitionChecks
{
    public static int Run(Catalog catalog)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
            checks++;
        }
        var scarlet = new GameDefinition("scarlet", "Scarlet", "Scarlet / Violet");
        var violet = new GameDefinition("violet", "Violet", "Scarlet / Violet");
        PokemonVariant Variant(string id, string form = "") => new() { Id = id, Name = id, Form = form, NationalNumber = 25, SpriteId = 25 };
        DexDefinition Dex(string id, params string[] entries) => new() { Id = id, Name = id, Group = "fixture", SourceDexIds = [id], Entries = entries.Select(p => new DexEntry(p, 1)).ToList() };
        var treecko = Variant("treecko");
        var dexes = new[] { Dex("paldea"), Dex("kitakami"), Dex("blueberry", "treecko") };
        var variants = new Dictionary<string, PokemonVariant> { ["treecko"] = treecko };
        const string html = """
            <table><tr><td><h2>Locations</h2></td></tr>
            <tr><td>Scarlet</td><td class="fooinfo">Transfer from Pokémon HOME or Trade with players with The Indigo Disk</td></tr>
            <tr><td>Violet</td><td class="fooinfo">Transfer from Pokémon HOME or Trade with players with The Indigo Disk</td></tr>
            <tr><td rowspan="2">The Indigo Disk</td><td>Scarlet</td><td class="fooinfo"><a href="/pokearth/paldea/canyonbiome.shtml">Canyon Biome</a></td></tr>
            <tr><td>Violet</td><td class="fooinfo"><a href="/pokearth/paldea/canyonbiome.shtml">Canyon Biome</a></td></tr></table>
            """;
        foreach (var game in new[] { scarlet, violet })
        {
            var sources = AcquisitionExtractor.Extract(html, game, treecko, dexes, variants, "https://www.serebii.net/pokedex-sv/treecko/");
            Check(sources.Keys.SequenceEqual(["blueberry"]) && sources["blueberry"].Areas.SequenceEqual(["Canyon Biome"]) && sources["blueberry"].Method == "Encounter", "A native DLC encounter does not inherit a base transfer/trade method, including the rowspan edition.");
        }
        var pikachu = Variant("pikachu");
        dexes = [Dex("paldea", "pikachu"), Dex("kitakami", "pikachu"), Dex("blueberry")];
        variants = new() { ["pikachu"] = pikachu };
        const string mixed = """
            <table><tr><td><h2>Locations</h2></td></tr>
            <tr><td>Scarlet</td><td class="fooinfo"><a href="/pokearth/paldea/southprovince.shtml">South Province</a><br>Tera Raid Battles</td></tr>
            <tr><td>The Teal Mask</td><td>Scarlet</td><td class="fooinfo"><a href="/pokearth/kitakami/applehills.shtml">Apple Hills</a></td></tr>
            <tr><td>Legends: Z-A</td><td class="fooinfo"><a href="/legendsz-a/wildzone3.shtml">Wild Zone 3</a></td></tr>
            <tr><td>Legends: Z-A Mega Dimension</td><td class="fooinfo">Hyperspace Lumiose <a href="/legendsz-a/hyperspacewildzone/electric.shtml">Electric (2 Star)</a></td></tr>
            <tr><td>Brilliant Diamond</td><td class="fooinfo"><a href="/pokearth/sinnoh/trophygarden.shtml">Trophy Garden</a></td></tr>
            <tr><td>Legends: Arceus</td><td class="fooinfo"><a href="/pokearth/hisui/naturespantry.shtml">Nature's Pantry</a></td></tr></table>
            """;
        var extracted = AcquisitionExtractor.Extract(mixed, scarlet, pikachu, dexes, variants, "https://www.serebii.net/pokedex-sv/pikachu/");
        Check(extracted["paldea"].Areas.SequenceEqual(["South Province"]) && extracted["paldea"].Method == "Encounter / Raid battles" && extracted["kitakami"].Areas.SequenceEqual(["Apple Hills"]), "Wild encounters and raids remain visible without mixing DLC areas.");
        var za = AcquisitionExtractor.Extract(mixed, new("legends-za", "Legends: Z-A", "Legends: Z-A"), pikachu, [Dex("lumiose", "pikachu"), Dex("hyperspace", "pikachu")], variants, "https://www.serebii.net/pokedex-sv/pikachu/");
        Check(za["lumiose"].Areas.SequenceEqual(["Wild Zone 3"]) && za["hyperspace"].Areas.SequenceEqual(["Hyperspace Lumiose"]), "Z-A has its own scopes and excludes type/star category labels from ranked locations.");
        foreach (var (game, scope, area) in new[] { (new GameDefinition("brilliant-diamond", "Brilliant Diamond", "Brilliant Diamond / Shining Pearl"), "sinnoh", "Trophy Garden"), (new GameDefinition("legends-arceus", "Legends: Arceus", "Legends: Arceus"), "hisui", "Nature's Pantry") })
        {
            var sources = AcquisitionExtractor.Extract(mixed, game, pikachu, [Dex(scope, "pikachu")], variants, "https://www.serebii.net/");
            Check(sources.Keys.SequenceEqual([scope]) && sources[scope].Areas.SequenceEqual([area]), "An unrelated game's preceding DLC header cannot change acquisition scope.");
        }
        const string swordHtml = """
            <table><tr><td><b>Location</b></td></tr>
            <tr><td>Sword</td><td class="fooinfo"><a href="/pokearth/galar/route1.shtml">Route 1</a></td></tr>
            <tr><td>Isle of Armor</td><td>Sword</td><td class="fooinfo"><a href="/pokearth/galar/fieldsofhonor.shtml">Fields of Honor</a></td></tr>
            <tr><td>Crown Tundra</td><td>Sword</td><td class="fooinfo">Dynamax Adventures</td></tr></table>
            """;
        var sword = AcquisitionExtractor.Extract(swordHtml, new("sword", "Sword", "Sword / Shield"), pikachu,
            [Dex("galar", "pikachu"), Dex("isle-of-armor", "pikachu"), Dex("crown-tundra", "pikachu")], variants, "https://www.serebii.net/pokedex-swsh/pikachu/");
        Check(sword["galar"].Areas.SequenceEqual(["Route 1"]) && sword["isle-of-armor"].Areas.SequenceEqual(["Fields of Honor"]) && sword["crown-tundra"].Areas.SequenceEqual(["Max Lair"]), "Sword sources follow the content header, including encounters without map links.");
        var ordinary = Variant("meowth");
        var regional = Variant("meowth-galar", "Galarian");
        const string forms = """
            <table><tr><td><h2>Locations</h2></td></tr><tr><td>Scarlet</td><td class="fooinfo">
            Kantonian Form: <a href="/pokearth/paldea/area1.shtml">Area 1</a><br>
            Galarian Form: Gift at the Academy</td></tr></table>
            """;
        var formVariants = new Dictionary<string, PokemonVariant> { [ordinary.Id] = ordinary, [regional.Id] = regional };
        var formDexes = new[] { Dex("paldea", ordinary.Id, regional.Id), Dex("kitakami"), Dex("blueberry") };
        var regionalSources = AcquisitionExtractor.Extract(forms, scarlet, regional, formDexes, formVariants, "https://www.serebii.net/pokedex-sv/meowth/");
        Check(regionalSources["paldea"].Areas.Count == 0 && regionalSources["paldea"].Method == "Gift", "A regional gift does not inherit the ordinary form's encounters.");
        const string eventHtml = "<table><tr><td><h2>Locations</h2></td></tr><tr><td>Scarlet</td><td class=\"fooinfo\">Event</td></tr></table>";
        Check(AcquisitionExtractor.Extract(eventHtml, scarlet, pikachu, dexes, variants, "https://www.serebii.net/")["paldea"].Method == "Event" &&
            AcquisitionExtractor.Extract(eventHtml, scarlet, treecko, dexes, variants, "https://www.serebii.net/").Count == 0, "Numbered historical events remain, while event-only sources outside the numbered Dex are excluded.");

        DexDefinition CatalogDex(string id) => catalog.Dexes.Single(d => d.Id == id);
        PokemonVariant Pokemon(string id) => catalog.Pokemon.Single(p => p.Id == id);
        var actualPikachu = Pokemon("pikachu");
        Check(actualPikachu.SourceFor("scarlet", CatalogDex("paldea"))!.Areas.Contains("South Province Area Two") && !actualPikachu.SourceFor("scarlet", CatalogDex("paldea"))!.Areas.Contains("Apple Hills"), "Paldea Pikachu excludes Kitakami locations.");
        Check(actualPikachu.SourceFor("scarlet", CatalogDex("kitakami"))!.Areas.Contains("Apple Hills") && !actualPikachu.SourceFor("scarlet", CatalogDex("kitakami"))!.Areas.Contains("South Province Area Two"), "Kitakami Pikachu excludes Paldea locations.");
        Check(Pokemon("treecko").SourceFor("scarlet", CatalogDex("blueberry"))!.Method == "Encounter", "Generated Treecko sources describe the native Blueberry encounter.");
        Check(Pokemon("tauros").SourceFor("scarlet", CatalogDex("kitakami"))!.Areas.Count == 0 && Pokemon("tauros").SourceFor("scarlet", CatalogDex("kitakami"))!.Method == "Breed Paldean Tauros in Kitakami" &&
            Pokemon("tauros").SourceFor("scarlet", CatalogDex("blueberry"))!.Areas.SequenceEqual(["Savanna Biome"]), "Kitakami Tauros does not borrow a Blueberry encounter.");
        Check(Pokemon("moltres-galar").SourceFor("sword", CatalogDex("crown-tundra"))!.Areas.SequenceEqual(["Isle of Armor"]) && Pokemon("moltres-galar").SourceFor("sword", CatalogDex("isle-of-armor")) is null, "A Crown quest stays in Crown scope even when its encounter map is elsewhere.");
        var ranking = AcquisitionRanking.Build([actualPikachu, actualPikachu], "scarlet", CatalogDex("kitakami"));
        Check(ranking.Locations.Any(l => l.Area == "Apple Hills" && l.Pokemon.Count == 1) && ranking.Locations.All(l => l.Area != "South Province Area Two"), "Source rankings use scoped records and count each identity once.");
        var combined = actualPikachu.SourceFor("scarlet", CatalogDex("sv-complete"))!;
        Check(combined.Areas.Contains("Apple Hills") && combined.Areas.Contains("South Province Area Two"), "Complete trackers combine their component Dex sources.");
        Check(Pokemon("pikachu").SourceFor("firered", CatalogDex("frlg-national"))!.Method == "Encounter", "A National tracker does not add a fallback method to a valid component source.");
        foreach (var pokemon in catalog.Pokemon)
            foreach (var gameSources in pokemon.Sources)
                foreach (string scope in gameSources.Value.Keys)
                    Check(catalog.Dexes.Any(d => d.Id == scope && d.Group == catalog.Games.Single(g => g.Id == gameSources.Key).Group), "Every acquisition scope belongs to its owning game's Dex group.");
        return checks;
    }
}
