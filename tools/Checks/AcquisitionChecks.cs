using PokedexTracker.CatalogGenerator;
using PokedexTracker.Core;

internal static class AcquisitionChecks
{
    public static int RunFixtures()
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
        Check(za["lumiose"].Areas.SequenceEqual(["Wild Zone 3"]) && za["hyperspace"].Areas.SequenceEqual(["Electric (2 Star)", "Hyperspace Lumiose"]), "Z-A keeps Hyperspace rift types and star ratings within their own scope.");
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
        const string raidHtml = """
            <table><tr><td><h2>Locations</h2></td></tr>
            <tr><td>Scarlet</td><td class="fooinfo"><a href="/pokearth/paldea/area1.shtml">Area 1</a><br />Tera Raid Battles: <a href="/scarletviolet/teraraidbattles/2star.shtml">2 Star Raid Battles</a>, <a href="/scarletviolet/teraraidbattles/3star.shtml">3 Star Raid Battles</a></td></tr>
            <tr><td>Sword</td><td class="fooinfo"><a href="/pokearth/galar/route4.shtml">Route 4</a><br />Max Raid Battles: <a href="/pokearth/galar/giant'scap.shtml">Giant's Cap</a><br />Gigantamax Raid Battles: <a href="/pokearth/galar/giant'scap.shtml">Giant's Cap</a></td></tr></table>
            """;
        var tera = AcquisitionExtractor.Extract(raidHtml, scarlet, pikachu, dexes, variants, "https://www.serebii.net/pokedex-sv/pikachu/")["paldea"];
        Check(tera.Areas.SequenceEqual(["Area 1"]) && tera.Raids.Select(r => r.Detail).SequenceEqual(["2 Star Raid Battles", "3 Star Raid Battles"]) && tera.Raids.All(r => r.Method == "Tera Raid Battles" && r.Url.StartsWith("https://www.serebii.net/scarletviolet/teraraidbattles/")), "Tera Raid star ratings and links survive independently of ordinary locations.");
        var max = AcquisitionExtractor.Extract(raidHtml, new("sword", "Sword", "Sword / Shield"), pikachu, [Dex("galar", "pikachu")], variants, "https://www.serebii.net/pokedex-swsh/pikachu/")["galar"];
        Check(max.Areas.SequenceEqual(["Route 4"]) && max.Raids.Select(r => r.Method).SequenceEqual(["Max Raid Battles", "Gigantamax Raid Battles"]) && max.Raids.All(r => r.Detail == "Giant's Cap"), "Max and Gigantamax raid locations do not become ordinary encounter areas.");
        Check(max.Raids.All(r => r.Url == "https://www.serebii.net/pokearth/galar/giant'scap.shtml"), "Raid links preserve apostrophes inside quoted URLs.");
        var raidOnly = pikachu with { Sources = new() { ["sword"] = new() { ["galar"] = max with { Areas = [] } } } };
        var raidRanking = AcquisitionRanking.Build([raidOnly, raidOnly], "sword", Dex("galar", "pikachu"));
        Check(raidRanking.Locations.Count == 0 && raidRanking.WithoutLocation.Count == 0 && raidRanking.Raids.Count == 2 && raidRanking.Raids.All(r => r.Pokemon.Count == 1), "Raid-only Pokémon appear once per raid method and stay out of ordinary and unlocated lists.");
        Check(AcquisitionSource.Combine([tera, tera]).Raids.Count == 2 && AcquisitionSource.Combine([max, max]).Raids.Count == 2, "Combined scopes preserve distinct raid details without duplicating them.");
        const string raidTable = """
            <h2><a name="paldea"></a>Paldea</h2>
            <tr><td class="pkmn"><img src="/scarletviolet/pokemon/new/small/128-b.png" /></td><td class="pkmn"><img src="/scarletviolet/pokemon/new/small/128-a.png" /></td></tr>
            <tr><td>Tauros</td><td>Tauros</td></tr><tr><td><b>Game?</b><br />Scarlet<td><b>Game?</b><br />Violet</tr>
            <h2><a name="terarium"></a>Terarium</h2>
            <tr><td class="pkmn"><img src="/scarletviolet/pokemon/new/small/103-a.png" /></td></tr>
            <tr><td>Exeggutor</td></tr><tr><td><b>Game?</b><br />Both</tr>
            """;
        var raidVariants = new[] { Variant("tauros-paldea-blaze-breed", "Paldean Blaze Breed") with { NationalNumber = 128 }, Variant("tauros-paldea-aqua-breed", "Paldean Aqua Breed") with { NationalNumber = 128 }, Variant("exeggutor-alola", "Alolan") with { NationalNumber = 103 } };
        Check(TeraRaidExtractor.Extract(raidTable, raidVariants).SequenceEqual([new("tauros-paldea-blaze-breed", "paldea", "scarlet"), new("tauros-paldea-aqua-breed", "paldea", "violet"), new("exeggutor-alola", "blueberry", "scarlet"), new("exeggutor-alola", "blueberry", "violet")]), "Raid tables retain form identities, edition exclusivity, and DLC scope.");
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
        string formRaids = forms.Replace("Gift at the Academy", "Gift at the Academy<br />Tera Raid Battles: <a href=\"/scarletviolet/teraraidbattles/2star.shtml\">2 Star Raid Battles</a>");
        var raidDex = Dex("paldea", ordinary.Id) with { Entries = [new(ordinary.Id, 1), new(regional.Id, 2, true)] };
        Check(AcquisitionExtractor.Extract(formRaids, scarlet, ordinary, [raidDex], formVariants, "https://www.serebii.net/")["paldea"].Raids.Count == 1 &&
            AcquisitionExtractor.Extract(formRaids, scarlet, regional, [raidDex], formVariants, "https://www.serebii.net/")["paldea"].Raids.Count == 0, "An unlabelled raid belongs to the numbered native form, not a regional gift.");
        var regionalNativeDex = Dex("paldea", regional.Id) with { Entries = [new(regional.Id, 1), new(ordinary.Id, 2, true)] };
        Check(AcquisitionExtractor.Extract(formRaids, scarlet, regional, [regionalNativeDex], formVariants, "https://www.serebii.net/")["paldea"].Raids.Count == 1, "Unlabelled raids remain available when the numbered native form is regional.");
        const string eventHtml = "<table><tr><td><h2>Locations</h2></td></tr><tr><td>Scarlet</td><td class=\"fooinfo\">Event</td></tr></table>";
        Check(AcquisitionExtractor.Extract(eventHtml, scarlet, pikachu, dexes, variants, "https://www.serebii.net/")["paldea"].Method == "Event" &&
            AcquisitionExtractor.Extract(eventHtml, scarlet, treecko, dexes, variants, "https://www.serebii.net/").Count == 0, "Numbered historical events remain, while event-only sources outside the numbered Dex are excluded.");

        return checks;
    }

    public static int RunCatalog(Catalog catalog)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
            checks++;
        }
        DexDefinition CatalogDex(string id) => catalog.Dexes.Single(d => d.Id == id);
        PokemonVariant Pokemon(string id) => catalog.Pokemon.Single(p => p.Id == id);
        var actualPikachu = Pokemon("pikachu");
        Check(actualPikachu.SourceFor("legends-za", CatalogDex("hyperspace"))!.Areas.SequenceEqual(["Hyperspace Lumiose", "Electric (2 Star)", "Electric (4 Star)"]), "Generated Pikachu retains both Hyperspace rift ratings.");
        Check(Pokemon("slowpoke-galar").SourceFor("legends-za", CatalogDex("hyperspace"))!.Areas.SequenceEqual(["Hyperspace Lumiose", "Poison (5 Star)", "Psychic (3 Star)"]), "Regional encounter overrides do not replace extracted Hyperspace rift details.");
        Check(actualPikachu.SourceFor("scarlet", CatalogDex("paldea"))!.Raids.Select(r => r.Detail).SequenceEqual(["2 Star Raid Battles", "3 Star Raid Battles"]), "Generated Pikachu retains both Tera Raid star ratings.");
        Check(actualPikachu.SourceFor("sword", CatalogDex("galar"))!.Areas.SequenceEqual(["Route 4", "Rolling Fields", "Stony Wilderness"]) && actualPikachu.SourceFor("sword", CatalogDex("galar"))!.Raids.Any(r => r.Detail == "Giant's Cap" && r.Url.EndsWith("giant'scap.shtml")), "Generated Max Raid locations and links remain distinct from ordinary encounters.");
        Check(Pokemon("venusaur").SourceFor("sword", CatalogDex("isle-of-armor"))!.Areas.Count == 0 && Pokemon("venusaur").SourceFor("sword", CatalogDex("isle-of-armor"))!.Raids.Count == 4, "Raid-only Venusaur has Max and Gigantamax sources without ordinary encounter locations.");
        Check(Pokemon("tauros-paldea").SourceFor("scarlet", CatalogDex("paldea"))!.Raids.Select(r => r.Detail).SequenceEqual(["4 Star Raid Battles", "6 Star Raid Battles"]) && Pokemon("tauros-paldea-blaze-breed").SourceFor("scarlet", CatalogDex("paldea"))!.Raids.Select(r => r.Detail).SequenceEqual(["5 Star Raid Battles", "6 Star Raid Battles"]) && Pokemon("tauros-paldea-blaze-breed").SourceFor("violet", CatalogDex("paldea"))!.Raids.Count == 0, "Combat and Blaze Tauros keep their own raid ratings and edition restrictions after source overrides.");
        Check(Pokemon("exeggutor-alola").SourceFor("scarlet", CatalogDex("blueberry"))!.Raids.Select(r => r.Detail).SequenceEqual(["6 Star Raid Battles"]), "The extra Alolan Exeggutor identity retains its own raid rating without the ordinary form's five-star raids.");
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
