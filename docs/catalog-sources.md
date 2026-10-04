# Catalog sources and form policy

The checked-in catalog contains 13 edition choices, 18 Pokédex choices, and 1,083 Pokémon identities. The National list has 1,025 species followed by 58 extra regional entries. Other lists are numbered regional or game-specific National lists, plus their compatible regional extras and distinct regional evolutions.

PokéAPI dex membership and English species names are pinned to commit `bc92d3b6029ef1abe9e7ad424c400b338f3c11fe`. Sprite files are pinned to `a3a1432e688ea028f12c51371d5253037cb9f17b`. The generator records all acquisition source URLs in `wwwroot/data/provenance.json`.

Form compatibility and numbered regional slots were cross-checked against [PKHeX personal tables](https://github.com/kwsch/PKHeX/tree/master/PKHeX.Core/Resources/byte/personal) and their corresponding decoders. No PKHeX code or binary data is bundled in this application. Its form-specific dex fields are useful because species-level PokéAPI entries do not distinguish native regional forms. [SV encounter data](https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/Resources/legality/wild/Gen9/encounter_wild_paldea.pkl) and [Z-A encounter data](https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/Legality/Encounters/Data/Gen9/Encounters9a.cs) provided independent checks of local availability.

Notable policies:

- Ordinary and Alolan Exeggutor both share Blueberry number 004. This project uses ordinary Exeggutor in the numbered slot and Alolan Exeggutor among extras, as Jeff requested.
- Blueberry uses Alolan Diglett/Dugtrio and Galarian Slowpoke/Slowbro/Slowking in their numbered slots. Ordinary counterparts appear among extras.
- Hisui uses its native Hisuian forms and White-striped Basculin. Alolan Vulpix/Ninetales and ordinary Sneasel supplement the list. Unsupported ordinary Growlithe and other counterparts are excluded.
- White-striped Basculin has its own stable identity. Red and Blue stripes share ordinary Basculin's checklist entry.
- Regional Tauros Combat, Blaze, and Aqua breeds are supported. Cosmetic, gender, shiny, Mega, Gigantamax, and other temporary battle forms are excluded.
- Compatibility does not establish a wild encounter. Compatible regional variants without a verified local route are labeled for transfer or regional evolution, rather than receiving the ordinary form's encounter areas.

Location extraction is limited to factual route/area names and short acquisition categories. Reviewed overrides cover regional gifts, trades, and cases where the source page omits a game record. Serebii links provide the detailed conditions. Edition exclusivity can require trades even when an area has a fixed encounter of another form; consult the linked source for exact conditions.

The generator and checks are rerunnable. Catalog changes require a version increase and a review of membership, native forms, availability, and combined ordering.
