<img src="assets/icon.svg" width="64" height="64" alt="">

# Pokédex tracker

A standalone .NET 10 Blazor WebAssembly checklist for the mainline Pokémon games on Switch. Track each regional Pokédex, DLC list, combined collection, or the full National Pokédex. Create independent trackers for completion, living dexes, or any goal you choose.

[Open the tracker](https://trowgundam.github.io/pokedex-tracker/).

Game and DLC checklists include only natively obtainable Pokémon, including trades between paired editions. They exclude transfer-only entries. Numbered event entries remain after distributions expire; event-only extra forms are excluded. The full cross-game National Dex includes every Pokémon.

Progress stays in the browser. Optional folder sync writes directly to a folder you select; software such as Syncthing transfers it between your devices. The website has no progress database, login, analytics, or cloud credentials.

## Run locally

Install the SDK specified by `global.json`, then run:

```sh
dotnet run --project pokedex_tracker
```

The console prints the development URL. Folder access requires a secure context and a browser that exposes the File System Access API and Web Locks. Brave can disable this API. If it is unavailable, local tracking and backup import/export still work.

## Use folder sync

1. Select a dedicated folder on each device, managed by your chosen synchronization tool.
2. Choose **Sync now** to save local edits and import folder changes. Reconnect after reopening the app if browser permission is required.
3. Review any per-tracker conflicts before choosing a version. File timestamps are shown for context; the app does not silently choose the newest state.

Each tracker has one current JSON file named `tracker-<id>-<SHA-256>.json`. Saves create and verify the replacement before removing acknowledged old files. Different concurrent files or a filename/content mismatch trigger a warning. The hash detects content changes; it is not an authentication mechanism.

Deleting a synchronized tracker removes its folder file. Other devices ask whether to accept the deletion or recreate their local tracker. There are no folder tombstones or application-managed snapshots. Export backups before destructive actions. Imports create independent copies.

A folder save confirms local filesystem storage, not delivery to another device. External synchronization tools do not provide an atomic lock with the browser. See [the synchronization design](docs/sync-options.md).

## Verify and publish

```sh
dotnet build pokedex_tracker.slnx
dotnet run --project tools/Checks -- .
node --test tools/preferences-checks.mjs
node --check pokedex_tracker/wwwroot/app.js
node --check pokedex_tracker/wwwroot/layout.js
node --check tools/browser-checks.js
node --check tools/browser-sync-checks.js
node --check tools/browser-ui-checks.js
node --check tools/browser-catalog-checks.js
node --check tools/browser-theme-checks.js
node --check tools/browser-evolution-checks.js
dotnet workload install wasm-tools
dotnet publish pokedex_tracker -c Release -o artifacts/publish
dotnet run --project tools/Publish -- artifacts/publish/wwwroot /pokedex-tracker/
node tools/browser-ci.mjs artifacts/publish/wwwroot /pokedex-tracker/
```

The browser runner requires Node 22 or later and an installed Chrome or Chromium executable. Set `CHROME_BIN` if the runner cannot find the browser. It serves the prepared output beneath the specified base path and uses a disposable browser profile. Run local UI checks in the Arch KDE VM unless you explicitly choose another environment. The publish tool updates the base URL and service-worker integrity metadata together. The release service worker caches the app, catalog, and sprites so a successful first visit supports offline reopening. Development builds deliberately do not cache.

The [Pages workflow](.github/workflows/pages.yml) validates and deploys `main` to GitHub Pages. Set the repository's Pages source to **GitHub Actions**. Pull requests show separate catalog, generation, tracker, JavaScript, release-build, and browser categories. The jobs share compiled check tools and one prepared website instead of rebuilding in each test job. The three required check names remain stable. Documentation-only changes skip application work while completing those required checks. Deployment uses the tested website artifact and runs only on `main`; PRs never deploy. See [contribution requirements](CONTRIBUTING.md) for check categories, caches, diagnostic artifacts, and local category commands.

The [browser filesystem checks](tools/browser-checks.js) run in a disposable local preview origin's console. They use real IndexedDB and file handles in OPFS, substituting only the operating-system directory picker. They do not prove a particular browser's native picker or external Syncthing delivery.

The [sync interface checks](tools/browser-sync-checks.js) drive tracker creation, unchanged sync, corrupt-file conflict resolution, and deletion through the real Blazor interface. They use real IndexedDB and isolated OPFS files and require an empty disposable preview origin.

The [interface checks](tools/browser-ui-checks.js) drive real Blazor DOM events for tracker navigation, Settings, creation, acquisition information, filtering, all theme choices, independence, rename, reset, and deletion. They create and remove their own verification trackers. Run them only in a disposable local preview origin.

The [catalog interface checks](tools/browser-catalog-checks.js) create trackers to verify native counts, exact Scarlet and Violet extras, DLC boundaries, acquisition links, numbered event entries, and the unrestricted cross-game National list. They remove their own trackers afterward.

The [theme checks](tools/browser-theme-checks.js) exercise both visual themes and all color schemes across every edition. They check all 14 Catppuccin accents, navigation, neutral fallback, saved accent choices, and text and primary-action contrast.

Acquisition records belong to a game and a regional or DLC Dex. Both the Sources ranking and information panel select records for the active Dex. Complete trackers combine their component Dexes; National trackers combine the applicable regional records. Scope follows the required content, so a Crown Tundra quest remains a Crown source even when its encounter happens on another map. Records without a listed location remain available separately.

## Appearance

The Sources pane shows verified evolution families below acquisition details, with local sprites, names, and requirements. It highlights the selected Pokémon and keeps regional families separate. National trackers label trees by game family. Requirements cover special conditions and reviewed game-specific alternatives. Trees omit methods that require another game or region. The generator inventories native family paths before filtering source rules. Missing methods become a research worklist that must be completed before replacing the catalog. See [catalog sources](docs/catalog-sources.md) for the coverage policy.

The tracker sidebar collapses to an Expand icon and a Settings gear. The browser remembers its collapsed state. Main boxes show position ranges such as `001-030`; Extra Forms keep their box labels. Unchecked sprites are grayscale. Checked cards have colored sprites, an accent-tinted background, and a stronger accent border. Acquisition information opens in a right-side panel that updates when you select another Pokémon.

Settings separates **Theme**, **Accent color**, and **Color scheme**. Catppuccin uses Latte and Mocha and offers all 14 [official accent colors](https://github.com/catppuccin/palette/blob/main/palette.json). Normal uses neutral light and dark surfaces; its accent follows the displayed tracker's edition, with blue for the welcome screen and full National tracker. Auto follows the browser's color scheme, including changes while the app is open. Theme, accent, and color-scheme preferences stay on the device. Switching away from Catppuccin preserves its accent choice. Existing Game Specific preferences map to Normal.

To add a theme, register its ID, display name, and available accents in [AppearanceThemes.cs](pokedex_tracker/Services/AppearanceThemes.cs), then define its color variables in [themes.css](pokedex_tracker/wwwroot/css/themes.css) under `:root[data-theme="your-id"]`. The first registered accent is the default. Select individual accents with `data-accent`; a theme with one accent omits the selector and can provide an `AccentDescription`. Each `light-dark(light, dark)` value supplies both variants. Layout uses semantic variables such as `--base`, `--text`, `--accent`, and `--accent-ink`. Accent fills use the original color; `--accent-ink` supplies readable link text and focus indicators, while `--on-accent` supplies button text. Game-dependent palettes can also select `data-game`; unknown games inherit the theme's base palette. Tracker and synchronization code need no changes.

## Maintain the catalog

```sh
dotnet run --project tools/CatalogGenerator -- .
dotnet run --project tools/Checks -- .
```

To update only evolution rules from the pinned PokéAPI CSV revision, run `dotnet run --project tools/CatalogGenerator -- . --evolutions-only`. This preserves checklist membership, acquisition records, sprites, and catalog version. The normal generator also includes this step. Use `--audit-evolutions` to collect the research worklist without replacing catalog data, and `--verify-evolutions` to check deterministic regeneration as CI does. See [evolution coverage and game onboarding](docs/catalog-sources.md#evolution-coverage-and-game-onboarding).

The generator pins PokéAPI CSV and sprite revisions, caches downloaded source pages under ignored `artifacts/catalog-cache`, and extracts factual location names and acquisition categories. Clear that cache to refresh Serebii pages. Review native form mappings and game or DLC obtainability when adding a game. Neither species-level dex membership nor HOME compatibility proves local availability.

Increase the catalog version after a membership or ordering correction. Saved checks use stable identities and survive catalog changes. New entries start unchecked. The interface explains that corrected entries may move boxes.

See [requirements](docs/requirements.md), [domain terms](CONTEXT.md), [catalog sources](docs/catalog-sources.md), and [verification results](docs/verification.md).

## Credits

Unofficial fan project. Pokémon names and sprites belong to Nintendo, Game Freak, and Creatures. Catalog data and sprites come from [PokéAPI](https://github.com/PokeAPI/pokeapi) and [PokeAPI/sprites](https://github.com/PokeAPI/sprites), with factual acquisition references linked to [Serebii](https://www.serebii.net/). The interface uses the [Catppuccin palette](https://github.com/catppuccin/palette). See [third-party notices](THIRD_PARTY_NOTICES.md).
