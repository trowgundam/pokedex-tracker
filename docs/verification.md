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

## Ranked acquisition sources

The Sources view ranks listed locations for the selected edition by distinct unchecked Pokémon. The cross-game National tracker ranks games instead, including entries obtained by evolution or other methods without a location. Regional variants retain their separate identities. This view uses the existing catalog and saved checks; no tracker or sync format changed.

All 9,724 catalog, ranking, and tracker checks passed. The optimized Release publish completed without warnings or errors, and all 1,137 offline asset hashes matched. The production bundle served under `/pokedex-tracker/` passed 13 source-view assertions and 41 interface assertions in T3's native browser. These cover overlapping-source updates, restoring counts from the box checklist, independent source-info actions, preserving expanded groups, game-only National grouping, and cleanup of each script's trackers. The browser scripts wait for saving to finish before attempting another edit.

Desktop and 390-pixel screenshots were captured and reviewed. Light and dark Sources layouts had no horizontal overflow. Location summaries were at least 56 pixels high, checklist rows were at least 60 pixels high, and information targets were 44 pixels wide. Counts describe listed availability, without assuming every entry can be caught directly or inferring evolution chains.

## Tracker URLs and remembered selection

Tracker selection uses the existing persistent GUID in a normalized `tracker` query parameter. Without that parameter, the app restores the last opened tracker and replaces the base address with its bookmarkable URL. The preference is device-local and does not change the tracker or sync formats. Renaming preserves a URL; duplication creates a new one. Unknown and invalid links preserve the last valid preference and display an unavailable message.

T3's native browser verified full reloads, restoring a non-first tracker from the base URL, explicit bookmark precedence, Back and Forward, stable rename URLs, independent duplicate URLs, deleting the active and last trackers, and fallback from a stale saved preference. Navigation stages are reusable through `tools/browser-navigation-checks.js`, with real page reloads between stages. A link to an absent tracker became active after folder sync imported that exact ID from a hash-verified OPFS file. Only the directory picker was substituted, and the test folder was removed afterward. Backup import continues to create independent copies with different URLs.

The final optimized Release publish completed without warnings or errors. All 1,137 offline asset hashes matched. The final production bundle served locally under `/pokedex-tracker/` passed all 41 interface checks and 13 Sources checks. The native preview host disconnected during a final optional narrow-viewport check; that check did not complete, and no alternative browser was used. The user approved a local commit after review. Publication remains pending.

## Full-project review repairs

The first two independent review passes covered all current project code against the local requirements and documented standards. Repairs use revision-specific PokéAPI and sprite caches, allow recovery of an explicitly acknowledged corrupt current file, coordinate local saves with folder writes across tabs, and refresh the last successful sync time without editing the checklist or rewriting unchanged folder content. Jeff chose to require Web Locks for folder sync; unsupported browsers retain local tracking and backups.

National game sources now include permanent native availability outside numbered regional lists, including Dynamax Adventures, Snacksworth rewards, reviewed gifts, breeding, and evolution. Both National Tauros breeds have valid source links and paired-edition availability. Regional and combined checklist membership and ordering are unchanged. Jeff chose to exclude expired distributions outside each game's numbered Dex from National source rankings.

The optimized Release bundle passed 9,733 core checks, 24 browser filesystem checks, seven real Blazor sync checks, and 17 Sources checks. All 11 JavaScript syntax checks and 1,137 published offline asset hashes passed. Filesystem checks use real IndexedDB and OPFS, with only the directory picker replaced. They verified stale-tab rejection, queued local saves during folder commits, unchanged sync metadata, corrupt-file acknowledgement, and safe capability rejection without Web Locks. An isolated full generator run rejected stale legacy CSV caches and replaced a stale published sprite; all 1,083 sprites matched the pinned inputs.

Native picker behavior and external Syncthing delivery were not tested. T3 native browser resize and screenshot calls remained intermittent; no alternative browser was used.

Jeff chose to restore one Pokémon per phone row, separated by box headings. All 44 interface assertions passed in a real 390×844 app frame within T3's native browser. A 320px frame also had no horizontal overflow, compact rows, and 44px source targets. These checks inspected the actual Blazor interface and computed geometry; screenshot-based visual review was not completed during this pass.

The third independent pass found that Dynamax Adventure availability was present but its method and Max Lair location were missing. The source parser now recognizes that permanent encounter category and omits historical Event methods outside each game's numbered Dex. Regeneration corrected source details for 261 species while preserving every checklist and identity. All 9,735 core checks and 18 Sources interface checks passed against a fresh optimized bundle, including Mewtwo's actual information panel. All published offline asset hashes matched.

The fourth independent pass corrected tracker terminology in startup/welcome text and tablet box geometry. A native 768px app frame reproduced the old three-column arrangement. The repaired optimized bundle passed all 43 non-phone interface assertions. Further measurements at 720, 768, 1024, and 1180 pixels with Sources open preserved six columns, separate checkbox/info targets, unobscured position numbers, and no page overflow. Very narrow non-phone content scrolls inside its box instead of reordering slots. All 1,137 offline asset hashes and JavaScript syntax/diff checks passed.

The fifth independent full-project pass reported zero actionable Standards findings and zero actionable Spec findings, ending the review loop. Both reviewers inspected the complete current tree, including all uncommitted repairs and new verification scripts. Their read-only checks passed the 9,735 existing Release assertions, all 11 JavaScript syntax checks, catalog/provenance consistency, unchanged checklist membership and stable identities, and diff checks. They did not independently rebuild or exercise the browser; the fresh optimized publication and native browser evidence above were produced by the primary agent. The repairs were uncommitted and unpublished when the review loop finished.

## Required CI checks

The Pages workflow has three independent jobs for catalog/tracker checks, JavaScript syntax, and optimized Release publication. The generator compiled with no warnings or errors, all 9,735 behavior assertions passed, every application/browser verification script parsed, and a fresh optimized publication verified all 1,137 offline asset hashes. A copied publication with a corrupted sprite was rejected by the publish tool. Workflow structure checks confirmed PR triggers, read-only default permissions, stable check names, and main-only deployment after all three jobs pass. The browser scripts are parsed, not executed, by CI.

## Dex-scoped sources and review corrections

Catalog version `2026-10-06.1` stores acquisition records by edition and regional or DLC Dex. The information panel and Sources rankings select the same records. Complete trackers combine their components, while National trackers combine the applicable scopes. Checklist membership, ordering, Pokémon identities, and sprite IDs match the preceding catalog. Quest sources follow their required DLC, including Crown Tundra birds encountered on another map. Missing source details stay visible without borrowing another Dex's locations.

The solution built without warnings or errors. All 16,565 catalog, HTML extraction, ranking, synchronization, and actual tracker-store assertions passed. Three Node preference tests passed, together with all JavaScript syntax and diff checks. The optimized Release publish succeeded, and all 1,140 prepared offline asset hashes matched.

T3's isolated preview exercised the prepared release under `/pokedex-tracker/`. The shared browser plan returned 426 assertions: 43 desktop interface checks, 44 phone interface checks, 45 catalog checks, 200 appearance checks, 23 scoped-source checks, seven synchronization checks, eight storage checks, 24 filesystem checks, and 32 navigation and offline checks. Appearance checks measured a minimum sampled contrast of 4.91:1. Storage checks aborted a real IndexedDB transaction and confirmed that the failed edit reverted while the preceding saved check remained. They also verified visible creation errors and duplication of a 120-character name.

Offline verification waited for a saved check in IndexedDB and the release service worker. With the static server stopped and a direct HTTP request unable to connect, a full page navigation reopened the tracker and retained that check. The server was then restarted and verification trackers removed. Filesystem checks used real IndexedDB and OPFS handles, with only the native picker substituted. No software was installed in the Arch KDE VM; it remains stopped.

CI now executes the same browser plan in a disposable Chrome profile and runs preference behavior tests. Its three protected job names remain unchanged. The Chrome/CDP runner passed syntax and independent source review; its transport has not been executed locally because Jeff selected T3's preview. Native directory selection and external Syncthing delivery remain untested. Verification finished on `fix/dex-scoped-sources-and-review-fixes` before commit and PR publication.
