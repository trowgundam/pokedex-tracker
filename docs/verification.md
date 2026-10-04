# Initial release verification

## Automated checks

- `dotnet build pokedex_tracker.slnx`: no warnings or errors.
- `dotnet run --project tools/Checks -- .`: 8,815 catalog and tracker behavior assertions passed.
- JavaScript syntax checks passed for the browser interop, service worker, and browser verification scripts.
- Optimized `dotnet publish` succeeded. The prepared Pages output has a `/pokedex-tracker/` base URL and matching integrity hashes for all 1,135 offline-cache assets.

The catalog checks cover all game lists, numbered membership counts, stable identities, regional examples, combined deduplication and ordering, source links, and local sprites. Tracker checks cover independent changes, round trips, canonical hashes, baseline comparisons, changed remote state, conflicting copies, integrity mismatch, retry convergence, and deletion conflicts.

## Browser behavior

The product-native T3 preview exercised the real app and the locally served optimized release. Confirmed paths include tracker creation, marking and reload persistence, duplicate independence, search preserving slots, information dialogs, backup export/import as independent copies, a per-tracker local/remote conflict, and selecting the remote version without changing unrelated trackers.

The rerunnable UI script passed 18 assertions against the optimized app. It exercised creation, checks, information dialogs, search and checked filtering, all three theme settings, independent duplication, rename, and both canceling and confirming reset and deletion.

Desktop boxes contain 30 positions arranged in six columns. At a 390-pixel viewport, they become lists with the same box boundaries. The page does not overflow horizontally, and information buttons have 44-pixel touch targets. Catppuccin dark and light colors and System mode were checked against computed browser styles.

The rerunnable filesystem script passed 12 assertions using real IndexedDB and OPFS handles. It replaced only the OS picker with an isolated directory. It checked replacement cleanup, concurrent versions, stale-write rejection, Syncthing conflict-copy recognition, corrupt hashes, acknowledged recovery, deletion, and retries.

The release's first cached-catalog test exposed a streaming-response startup stall. Buffering the HTTP response fixed that path. With the static server stopped, the optimized app reopened, created a tracker, saved a check, and preserved that check across another offline reload.

## Boundaries

Native directory selection in Jeff's Brave, mounted network shares, and actual external Syncthing transfer between devices have not been exercised. The application handles an unavailable picker without disabling local tracking or backups. Cloud providers are outside this release.

UI automation used the T3 collaborative browser. The Arch KDE VM lacked a browser and Node; no guest software was installed. Its original stopped state was restored.

The final optimized app also saved a checked tracker to a verified folder file. Removing the file externally preserved the local checklist and showed a deletion conflict. Explicitly accepting the deletion removed the visible tracker and left the folder empty.

GitHub Pages deployment verification is recorded after publication below.
