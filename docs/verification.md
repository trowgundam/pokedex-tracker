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

## Native availability correction

Catalog version `2026-10-04.2` replaces compatibility-based extras with reviewed game and DLC membership. Numbered event entries remain, while event-only extras and transfer-only game entries are excluded. The full cross-game National list remains at 1,083 entries.

New assertions first reproduced the incorrect Paldea extras, DLC-only Lumiose forms, and Galar extras. After correction, the solution built without warnings or errors and all 9,716 catalog and tracker assertions passed. JavaScript syntax checks passed, and the optimized Pages build had matching integrity hashes for all 1,135 assets.

The new browser catalog script passed 45 assertions against the locally served optimized build. It created and removed trackers for the exact four Scarlet and Violet lists, all three Sword and Shield regions, Let's Go, BDSP National, FireRed National, Lumiose, and the full cross-game National list. It checked rendered entries, progress totals, excluded forms, and native acquisition information. The existing interface script also passed all 27 assertions.

An old-catalog backup with Sprigatito and transfer-only Alolan Raichu checked was imported through Settings. After a full reload, Sprigatito remained checked, Alolan Raichu was absent from the Paldea checklist, and progress was 1 of 404. IndexedDB retained both saved identities, and the interface displayed its catalog-change notice. The verification tracker was then removed. Catalog corrections preserve saved data while counting only current checklist entries.

## Visual themes

Settings now separates Theme from Color scheme. The theme script passed 102 assertions against both development and locally served optimized builds. It covered all 13 edition choices, light and dark game palettes, Auto, tracker switching, neutral welcome and HOME palettes, Catppuccin consistency, and cleanup after deleting the last tracker. Tested text, muted text, primary-button text, and accent contrast had a minimum ratio of 5.27:1. The existing 27 interface assertions also passed.

Live browser preference changes switched Auto between light and dark without a reload for both Catppuccin and Game Specific. A saved Game Specific, Dark preference survived a full optimized-app reload, restored Scarlet's palette, and retained the checked Sprigatito. At 390 pixels, Settings displayed both selectors with 44-pixel heights, the checklist used box-separated lists, and the page had no horizontal overflow. Verification trackers were removed.

The optimized Pages output includes `css/themes.css` in its offline manifest, with matching hashes for all 1,136 assets. New themes require a registry entry and CSS palette variables; they do not change tracker or sync behavior.

## Selected Design B and requested refinements

The selected layout is integrated into main with a collapsible sidebar, a right-side acquisition panel, main box ranges, and distinct checked-card states. The optimized release passed 28 interface assertions, 45 catalog interface assertions, and 96 theme assertions in T3's native browser. The interface checks include the collapsed Settings gear, unchanged Extra Forms labels, grayscale unchecked sprites, panel reuse, and Escape focus restoration. Sidebar collapse survived a reload. The same 28 interface assertions passed with a clean production bundle served locally under `/pokedex-tracker/`, including the new module imports.

At 1440×1000 and 390×844, the checklist and source panel had no horizontal overflow or clipped Pokémon labels. Phone source targets were at least 44 pixels, and the source panel left the Settings gear accessible. Sampled checklist text contrast passed 52 theme, edition, and color-scheme combinations, with a minimum of 5.12:1. The solution build and all 9,716 catalog and tracker checks passed.

Native screenshot capture failed on the T3 browser client. These results verify actual DOM behavior and computed layout and colors, but do not include a screenshot-based visual review. No alternative browser was used.

The redundant checked badge and its reserved bottom space were removed. A fresh Release publish passed. T3 native browser checks confirmed no badges, 8-pixel bottom padding, distinct checked backgrounds and borders, colored checked sprites, and grayscale unchecked sprites. Checking and unchecking restored the expected state. Desktop and phone layouts had no horizontal overflow.

## Theme accents

Normal replaces Game Specific with neutral light and dark surfaces and an accent that follows the current tracker. Catppuccin exposes all 14 official Latte/Mocha accents. Theme registration owns accent choices and optional descriptions; per-theme accent preferences stay in browser storage.

The optimized Release publish completed without warnings or errors. In T3's native browser, the theme script passed 194 assertions across every edition and all Catppuccin accents, including Auto, automatic game accents, neutral backgrounds, and theme-switch persistence. Sampled text and primary-button label contrast was at least 4.91:1. The interface script passed all 28 assertions against the final bundle. JavaScript syntax and diff checks passed.

A Peach, Dark Catppuccin preference survived a full reload, preserving the 148/246 demo checklist. A saved legacy Game Specific preference loaded as Normal with Scarlet's accent. At 390×844, Settings had no horizontal overflow and its selectors were 44 pixels high. Desktop and mobile screenshots were captured and reviewed using T3's native browser. Screenshot capture and viewport resizing succeeded during this pass; the earlier capture limitation above applies to the earlier verification only.

Checked-card shading now uses the selected accent, with the readable accent variant for its border. The fresh Release publish passed. T3 native browser checks covered all 14 Catppuccin accents and Scarlet’s automatic accent in light and dark modes, 30 states total. Checked-card text contrast was at least 4.71:1. Checking and unchecking restored the expected tint and sprite state. Desktop and phone screenshots were reviewed, with no phone horizontal overflow.

A subsequent Brave report of Light staying dark was traced to Dark Reader. The user's diagnostic showed the selection and saved preference as `light`, but the computed root and body `color-scheme` as `dark`, with Dark Reader active and no service worker. In T3, both themes applied Light correctly even under an emulated dark browser preference. The user chose to preserve Dark Reader control and disable the extension for localhost; no application appearance changes were made. The user confirmed Light worked in Brave after disabling the extension.

## Approved redesign release

The production bundle was rebuilt without demo seeding or diagnostic pages, prepared under `/pokedex-tracker/`, and served locally. The Release solution build had no warnings or errors, and all 9,716 catalog and tracker checks passed. All 1,137 offline asset hashes matched, including the new layout module. In T3's native browser, the production bundle passed 28 interface assertions, 200 theme assertions with a minimum sampled contrast of 4.91:1, and 45 catalog interface assertions. Its service worker activated successfully. These scripts removed their own trackers after verification.

## Whole-cell checking

Each Pokémon cell now uses one native checkbox label, with the source button outside it. The fresh Release publish completed without warnings or errors. The production bundle served under `/pokedex-tracker/` passed all 40 interface assertions in T3's native browser, including checking and unchecking through the background, sprite, number, form, and name without double toggles. Source buttons left progress unchanged.

Native mouse clicks in empty cell space toggled the checkbox at desktop and 390-pixel widths. Native keyboard Space restored the original state. The label filled the cell's interior at both widths, and the phone layout had no horizontal overflow. JavaScript syntax and diff checks passed.
