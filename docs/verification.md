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

## Published release

The public [repository](https://github.com/trowgundam/pokedex-tracker) deploys `main` to [GitHub Pages](https://trowgundam.github.io/pokedex-tracker/). Pages uses GitHub Actions with HTTPS enforced. The [initial build and deployment](https://github.com/trowgundam/pokedex-tracker/actions/runs/37242076822) passed all 8,815 checks and published commit `66d6aad` on October 4, 2026.

The published app passed the same 18 DOM control assertions. Its service worker activated and cached all 1,135 assets, proving the hosted integrity metadata matches the delivered files. A separate checked tracker survived a full page reload. Desktop boxes had 30 positions and six columns. At 390 pixels, the published page used box-separated lists with no horizontal overflow and 44-pixel information buttons. No loaded sprite failed. Verification trackers were removed after testing.

## Navigation revision

The updated UI script passed 27 assertions on desktop and at 390 pixels, including the locally served optimized release. It checks collapsed tracker navigation, Settings as the final dropdown action, dismissal by outside click and Escape, modal focus restoration, the centered Poké Ball, and storage and appearance controls inside Settings. A native Escape key also closed Settings and restored focus. The desktop checklist occupied the full content width with six box columns. The phone layout had no horizontal overflow.

Syncing through Settings wrote a hash-verified tracker to a real OPFS directory. Removing that file externally produced a conflict; the Settings review action closed the modal and exposed resolution in the checklist. Accepting deletion cleaned up the test tracker. Only the directory picker was substituted.

Release verification exposed a cached service-worker import retaining the preceding manifest. The publish tool now versions the manifest import URL, and the app registers updates with `updateViaCache: none`. Two consecutive preparation runs succeeded, all 1,135 asset hashes matched, and the worker import used the prepared manifest version.
