<img src="assets/icon.svg" width="64" height="64" alt="">

# Pokédex tracker

A standalone .NET 10 Blazor WebAssembly checklist for the mainline Pokémon games on Switch. Track each regional Pokédex, DLC list, combined collection, or the full National Pokédex. Create independent trackers for completion, living dexes, or any goal you choose.

Progress stays in the browser. Optional folder sync writes directly to a folder you select; software such as Syncthing transfers it between your devices. The website has no progress database, login, analytics, or cloud credentials.

## Run locally

Install the SDK specified by `global.json`, then run:

```sh
dotnet run --project pokedex_tracker
```

The console prints the development URL. Folder access requires a secure context and a browser that exposes the File System Access API. Brave can disable this API. If it is unavailable, local tracking and backup import/export still work.

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
node --check pokedex_tracker/wwwroot/app.js
node --check tools/browser-checks.js
dotnet workload install wasm-tools
dotnet publish pokedex_tracker -c Release -o artifacts/publish
dotnet run --project tools/Publish -- artifacts/publish/wwwroot /pokedex-tracker/
```

Serve the prepared output beneath the same `/pokedex-tracker/` path when testing locally. The publish tool updates the base URL and service-worker integrity metadata together. The release service worker caches the app, catalog, and sprites so a successful first visit supports offline reopening. Development builds deliberately do not cache.

The [Pages workflow](.github/workflows/pages.yml) validates and deploys `main` to GitHub Pages. Set the repository's Pages source to **GitHub Actions**. Pull requests run the build without deployment.

The [browser filesystem checks](tools/browser-checks.js) run in a disposable local preview origin's console. They use real IndexedDB and file handles in OPFS, substituting only the operating-system directory picker. They do not prove a particular browser's native picker or external Syncthing delivery.

The [interface checks](tools/browser-ui-checks.js) drive real Blazor DOM events for creation, acquisition information, filtering, all theme choices, independence, rename, reset, and deletion. They create and remove their own verification trackers. Run them only in a disposable local preview origin.

## Maintain the catalog

```sh
dotnet run --project tools/CatalogGenerator -- .
dotnet run --project tools/Checks -- .
```

The generator pins PokéAPI CSV and sprite revisions, caches downloaded source pages under ignored `artifacts/catalog-cache`, and extracts factual location names and acquisition categories. Clear that cache to refresh Serebii pages. Review native form mappings and compatibility when adding a game; species-level dex lists alone cannot identify regional forms.

Increase the catalog version after a membership or ordering correction. Saved checks use stable identities and survive catalog changes. New entries start unchecked. The interface explains that corrected entries may move boxes.

See [requirements](docs/requirements.md), [domain terms](CONTEXT.md), [catalog sources](docs/catalog-sources.md), and [verification results](docs/verification.md).

## Credits

Unofficial fan project. Pokémon names and sprites belong to Nintendo, Game Freak, and Creatures. Catalog data and sprites come from [PokéAPI](https://github.com/PokeAPI/pokeapi) and [PokeAPI/sprites](https://github.com/PokeAPI/sprites), with factual acquisition references linked to [Serebii](https://www.serebii.net/). The interface uses the [Catppuccin palette](https://github.com/catppuccin/palette). See [third-party notices](THIRD_PARTY_NOTICES.md).
