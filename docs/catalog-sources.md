# Catalog sources and form policy

The checked-in catalog contains 13 edition choices, 18 Pokédex choices, and 1,083 Pokémon identities. The full cross-game National list has 1,025 species followed by 58 extra regional entries. Game and DLC lists contain their obtainable numbered entries, plus obtainable regional extras and distinct regional evolutions.

PokéAPI dex membership and English species names are pinned to commit `bc92d3b6029ef1abe9e7ad424c400b338f3c11fe`. Sprite files are pinned to `a3a1432e688ea028f12c51371d5253037cb9f17b`. The generator records all acquisition source URLs in `wwwroot/data/provenance.json`.

Form compatibility and numbered regional slots were cross-checked against [PKHeX personal tables](https://github.com/kwsch/PKHeX/tree/master/PKHeX.Core/Resources/byte/personal) and their corresponding decoders. No PKHeX code or binary data is bundled in this application. Its form-specific dex fields are useful because species-level PokéAPI entries do not distinguish native regional forms. [SV encounter data](https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/Resources/legality/wild/Gen9/encounter_wild_paldea.pkl) and [Z-A encounter data](https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/Legality/Encounters/Data/Gen9/Encounters9a.cs) provided independent checks of local availability.

## Obtainability

Membership follows native acquisition in the selected game or DLC, including permanent gifts, in-game trades, breeding, evolution, and trades between paired editions. HOME, Bank, GO, and other game-family imports do not qualify. Trading an imported Pokémon does not establish a native source. Neither HOME compatibility nor species-level dex membership proves obtainability.

Numbered entries from expired in-game events remain, including Walking Wake, Iron Leaves, Zarude, Manaphy, Darkrai, and Shaymin. Event-only extra forms are excluded. National game-source rankings also exclude expired distributions outside that game's numbered Dex. Permanent quest rewards and save-data gifts qualify. Combined lists remain the union of component Pokédexes and their regional extras, rather than every compatible or unnumbered species.

Scope follows required content, not just the encounter map. Crown Tundra's roaming birds are Crown entries even when they appear in the Wild Area or Isle of Armor. Base-game breeding and evolution facilities can support DLC-native Pokémon.

Reviewed counts, excluding extra forms, are:

| Checklist | Numbered entries | Extra entries |
| --- | ---: | ---: |
| Let's Go Kanto | 151 | 18 |
| Galar | 400 | 4 |
| Isle of Armor | 211 | 47 |
| Crown Tundra | 210 | 41 |
| Sword and Shield Complete | 584 | 30 |
| Sinnoh | 151 | 0 |
| BDSP National | 491 | 0 |
| Hisui | 242 | 3 |
| Paldea | 400 | 4 |
| Kitakami | 200 | 3 |
| Blueberry | 243 | 3 |
| Scarlet and Violet Complete | 682 | 7 |
| Lumiose | 232 | 5 |
| Hyperspace | 132 | 16 |
| Legends Z-A Complete | 364 | 16 |
| Switch FireRed and LeafGreen Kanto | 150 | 0 |
| Switch FireRed and LeafGreen National | 215 | 0 |
| Full cross-game National | 1,025 | 58 |

### Scarlet and Violet

The exact extra lists are Paldea's Galarian Meowth, Perrserker, Johtonian Wooper, and Quagsire; Kitakami's Hisuian Growlithe, Hisuian Arcanine, and Kantonian Tauros; and Blueberry's Alolan Exeggutor, Alolan Meowth, and Alolan Persian. [Tauros's location record](https://www.serebii.net/pokedex-sv/tauros/) documents Kitakami breeding. [League Club trades](https://www.serebii.net/scarletviolet/leagueclubtrades.shtml) document Alolan Meowth from Salvatore. Nonnumbered regional families can qualify; a species does not need its own numbered slot in that DLC.

Wooper and Quagsire fold into Kitakami's numbered positions in Complete. Kantonian Tauros folds into Blueberry's numbered position.

### Sword and Shield

Galar extras are Kantonian Meowth, Galarian Slowpoke, Kantonian Mr. Mime, and Unovan Yamask. The ordinary forms have [base-game NPC trades](https://www.serebii.net/swordshield/ingametrade.shtml), and Galarian Slowpoke has the permanent Wedgehurst encounter.

Isle extras include regional families available through [Diglett rewards](https://www.serebii.net/swordshield/diglett.shtml), Regina trades, breeding, and evolution. Isle #003 uses ordinary Slowking. Galarian Slowking requires Crown's Galarica Wreath; the expired item distribution does not justify an Isle extra entry.

Crown extras include [Max Lair encounters](https://www.serebii.net/swordshield/dynamaxadventurespokemon.shtml), their offspring, and Crown-native regional raids and evolutions. Its five Alolan Max Lair catches are Raichu, Sandslash, Dugtrio, Persian, and Marowak, confirmed by [the form-specific encounter resource](https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/Resources/legality/wild/Gen8/encounter_swsh_underground.pkl). Alolan Exeggutor is absent, and Galarian Slowbro requires Isle's Galarica Cuff. Neither qualifies for Crown extras.

### Other games

- Let's Go excludes Meltan and Melmetal, which require GO import. Mew remains because its [Poké Ball Plus gift](https://pokemonletsgo.pokemon.com/en-us/pokeball-plus/index.html) distributes directly to the game. The 18 Alolan extras use [NPC trades](https://www.serebii.net/letsgopikachueevee/trade.shtml) and their evolutions.
- BDSP National excludes [transfer-only Celebi and Deoxys](https://www.serebii.net/brilliantdiamondshiningpearl/transferonly.shtml). Historical numbered gifts and their native offspring remain under the event policy.
- Switch FireRed and LeafGreen includes 150 Kanto species, 62 Johto species, and Azurill, Wynaut, and Deoxys. It excludes Mew and species requiring another generation-three title, including unreleased Altering Cave encounters. [Switch ticket gifts](https://www.serebii.net/fireredleafgreen/nintendoswitch.shtml) make Lugia, Ho-Oh, and Deoxys native. The [unobtainable list](https://www.serebii.net/fireredleafgreen/unobtainable.shtml) establishes other exclusions. Original National numbers remain intact when entries are omitted.
- Lumiose extras are Alolan Raichu, Galarian Slowpoke, Galarian Slowbro, Galarian Slowking, and Galarian Stunfisk, supplied by [trades](https://www.serebii.net/legendsz-a/ingametrades.shtml), [gifts](https://www.serebii.net/legendsz-a/giftpokemon.shtml), and [Side Mission 75 evolution items](https://www.serebii.net/legendsz-a/sidemissions/someunusualpokemon.shtml). Hisuian Sliggoo, Goodra, and Avalugg qualify only for Hyperspace and Complete.

## Form identity and ordering

- Ordinary and Alolan Exeggutor both share Blueberry number 004. This project uses ordinary Exeggutor in the numbered slot and Alolan Exeggutor among extras, as Jeff requested.
- Blueberry uses Alolan Diglett/Dugtrio and Galarian Slowpoke/Slowbro/Slowking in their numbered slots. Their transfer-only ordinary counterparts are excluded from this DLC's checklist.
- Hisui uses its native Hisuian forms and White-striped Basculin. Alolan Vulpix/Ninetales and ordinary Sneasel supplement the list. Unsupported ordinary Growlithe and other counterparts are excluded.
- White-striped Basculin has its own stable identity. Red and Blue stripes share ordinary Basculin's checklist entry.
- Regional Tauros Combat, Blaze, and Aqua breeds are supported. Cosmetic, gender, shiny, Mega, Gigantamax, and other temporary battle forms are excluded.
- Extra entries have reviewed native sources. Transfer-only variants do not receive checklist entries or the ordinary form's encounter areas.

Location extraction is limited to factual route/area names and short acquisition categories. Reviewed overrides cover regional gifts, trades, and cases where the source page omits a game record. Serebii links provide the detailed conditions. Edition exclusivity can require trades even when an area has a fixed encounter of another form; consult the linked source for exact conditions.

National game sources also cover native Pokémon outside numbered regional checklists. The generator reads the Dynamax Adventure and Snacksworth encounter lists independently of checklist membership, plus reviewed gifts, breeding, and evolutions such as Poké Ball Plus Mew, Keldeo, Cosmog, Poipole, the Hoenn starters, and Urshifu. Tauros's Blaze and Aqua breeds have their own native-edition and paired-edition trade sources. These source records do not add entries to regional or combined checklists.

The generator and checks are rerunnable. Membership or ordering changes require a catalog version increase. Review membership, native forms, availability, and combined ordering whenever regenerating the catalog.
