# Pokédex tracker requirements

This document records Jeff's confirmed requirements. Open decisions remain separate from confirmed behavior.

## Hosting and data ownership

- Build a standalone Blazor WebAssembly application using the existing .NET starter.
- Host the site publicly on GitHub Pages.
- The site operator must not store user progress.
- Users configure synchronization for access from a laptop, phone, tablet, and desktop. Signing in to a storage provider is acceptable.
- Jeff can maintain site-wide OAuth registrations. Users do not need to register their own cloud applications.
- Implement only a user-selected folder provider initially. Dropbox, Google Drive, and WebDAV are outside the initial implementation scope.
- Keep storage providers modular so adding Google Drive or another provider later does not require sweeping changes to tracker behavior.
- Use one active sync provider at a time.
- Jeff prefers FOSS options where practical. External software such as Syncthing can synchronize the selected folder.
- Jeff primarily uses Brave on his laptop and desktop. His Brave does not expose the expected configuration flag. Folder sync requires a secure context, read/write folder access, and Web Locks. Folder support remains capability-dependent; provide local tracking and backups when unavailable. Do not change his browser configuration.
- Work on `main`. Do not create another branch or worktree for this initial project.

## Synchronization

- Retain the baseline and time of the last successful synchronization when editing offline.
- If pending local edits exist and the remote state has changed since that baseline, prompt before resolving the conflict.
- Offer replacing local state with remote state and replacing remote state with local state. Other reconciliation options remain to be decided.
- Prevent loss of conflicting data without user acknowledgement. Do not silently choose the later edit.
- Detect and resolve conflicts per tracker. Editing unrelated trackers on different devices does not create a conflict between those trackers.
- Support editing an already-open page offline and reopening the application offline after its first successful online visit.
- Store one current state file per tracker. Do not create an application-managed snapshot history.
- Store the last edit timestamp in the tracker data and include a content hash in each current file's name. Warn when the contents do not match that hash.
- Delete synchronized files without a folder tombstone. Other devices ask whether to accept deletion or retain and recreate their local tracker.
- Synchronization is explicit through Sync now. Remember browser folder handles where possible and renew access when required.

## Game coverage

Support these games and all their DLC in the initial release:

- Let's Go Pikachu and Let's Go Eevee.
- Sword and Shield.
- Brilliant Diamond and Shining Pearl.
- Legends: Arceus.
- Scarlet and Violet.
- Legends: Z-A.
- FireRed and LeafGreen on Switch.

Other generations will be added later. The catalog must support every Pokédex available in each supported game and a full National Pokédex across games.

Game and DLC checklists cover only Pokémon that can originate there. Encounters, permanent gifts, in-game trades, breeding, evolution, and trading between paired editions qualify. Transfer-only entries do not qualify, including imported Pokémon subsequently traded to another player. Scope follows the required game or DLC content, even when a DLC quest sends a Pokémon to a base-game area. A DLC checklist can use base-game facilities for breeding and evolution.

Retain numbered entries from expired in-game event distributions, including Walking Wake, Iron Leaves, Zarude, and BDSP Manaphy. Exclude event-only extra regional forms. The full cross-game National Pokédex remains a checklist of every Pokémon, regardless of individual games' availability. Its game-source rankings exclude expired distributions outside that game's numbered Dex; permanent gifts and encounters outside numbered lists qualify.

## Checklists

- Users can create any number of trackers for each Pokédex.
- Creating a tracker creates independent progress. Changes never automatically mark entries in another tracker.
- A checkbox has no enforced interpretation. Users can use it for Pokédex completion, a living dex, or another purpose.
- Users can reset a tracker or create another tracker when changing their collection goal.
- Each game uses its own storage box layout. Scarlet and Violet use 30 slots arranged as five rows of six. Tablets and desktop views preserve these dimensions; only the phone list below changes the visual arrangement.
- The National Pokédex uses the Pokémon HOME box layout.
- Games without ordinary boxes, including Let's Go, use the Pokémon HOME box layout.
- On phones, show one Pokémon per row in a list separated by box boundaries, with a group header every 30 entries for HOME-sized boxes.
- Include naming, renaming, duplicating, resetting, deleting, searching, checked/unchecked filtering, and backup export/import. Preserve box positions when searching or filtering. Reset and delete require confirmation.
- Provide an information action for each Pokémon to find acquisition sources, following the reference site's interaction.
- Acquisition information lists route or area names by game and links to Serebii for details. Detailed encounter rates and walkthroughs are outside the initial scope.
- Acquisition records belong to a game and a regional or DLC Dex. Regional trackers use only that Dex's sources in both the information panel and location rankings. Complete trackers combine their component Dexes; National trackers combine the applicable regional records. Scope follows the required game or DLC content, including quests whose encounter locations lie elsewhere.
- A Sources view ranks locations by the number of distinct unchecked Pokémon available there, descending. Expand a location to view and check its outstanding entries; checking an entry updates every associated source. Entries without a listed location remain accessible separately. The full cross-game National Dex ranks games instead of locations, including availability through evolution and other methods.

## Regional entries and ordering

- Extra forms cover regional variants, not cosmetic forms, gender differences, shinies, or temporary battle transformations.
- Extra regional entries also include distinct evolutions of those variants, such as Perrserker and Quagsire.
- A combined Pokédex lists base-game entries first, then new entries added by each DLC in release order.
- Deduplication preserves distinct regional variants of the same species.
- An entry with a numbered DLC position occupies that position rather than appearing again among extra regional entries.
- Numbered regional entries take priority over extra regional entries when assembling a combined list, even when an extra entry is obtainable in the base game.
- Extra regional entries appear at the end in separate boxes, following the reference site.
- Automatically add new catalog entries unchecked at the end.
- Correct inaccurate ordering or form mappings when necessary, preserve checks by Pokémon identity, and show a notice when corrections change box positions.

For Scarlet and Violet, Kantonian Meowth and Paldean Wooper occur in the numbered Paldea list. The exact extra lists are:

- Paldea: Galarian Meowth, Perrserker, Johtonian Wooper, and Quagsire.
- Kitakami: Hisuian Growlithe, Hisuian Arcanine, and Kantonian Tauros. Breeding Paldean Tauros in Kitakami produces Kantonian Tauros.
- Blueberry: Alolan Exeggutor, Alolan Meowth, and Alolan Persian.

In the combined list, Johtonian Wooper and Quagsire occupy their Kitakami positions, and Kantonian Tauros occupies its Blueberry position.

White-Striped Basculin occupies Kitakami's ordinary Basculin position. In the combined Scarlet and Violet list, it is distinct from the ordinary Basculin entry in Paldea. Red-Striped and Blue-Striped Basculin share that ordinary entry rather than receiving separate checkboxes.

White-Striped Basculin is an explicit exception justified by its gameplay differences. Do not derive checklist identity generally from movesets or abilities: Red-Striped Basculin has Reckless while Blue-Striped Basculin has Rock Head, despite their shared checklist entry.

## Appearance

- Put trackers and creation in a collapsible left sidebar. When collapsed, show only an Expand icon and a Settings gear.
- Put appearance, folder sync, and backup controls in a Settings modal.
- Tracker links use the persistent ID in a `tracker` query parameter, so bookmarks survive renaming and work on GitHub Pages. With no tracker ID in the URL, restore the last opened tracker on that device, falling back to the first available tracker, and replace the base URL with its bookmarkable URL without adding a history entry. Invalid or unavailable links show a notice and preserve the requested URL for later sync. Backup imports retain their existing behavior of creating independent copies with new IDs and URLs. Selecting, creating, duplicating, or deleting a tracker updates the URL and local preference; browser Back and Forward follow explicit tracker links.
- Show acquisition information in a right-side panel with a close button. Selecting another Pokémon updates that panel.
- Label main boxes with position ranges such as `001-030`; preserve the Extra Forms box labels.
- Clicking anywhere in a Pokémon cell toggles its check, except the source-info button. Keep native checkbox keyboard and disabled behavior.
- Show unchecked Pokémon sprites in grayscale. Show checked Pokémon in color, with a card tint and stronger border that follow the selected accent.
- Provide dark and light modes.
- Default to the browser's reported system color scheme.
- Keep Auto, Light, and Dark as the color-scheme selection, independent of the visual theme.
- Offer Catppuccin and Normal themes. Preserve Catppuccin as the default, using Mocha in dark mode and Latte in light mode.
- Catppuccin offers all 14 palette accents. Remember its chosen accent when switching themes.
- Normal uses neutral light and dark surfaces. Its accent automatically follows the edition of the currently displayed tracker, including DLC and combined lists.
- Normal uses a blue accent on the welcome screen and for the full cross-game National tracker.
- Remember theme, per-theme accent, and color scheme on the device. Theme preferences do not alter checklist or sync data.
- Keep theme registration and palette definitions separate from tracker behavior so additional themes require no tracker or sync changes.

## Deferred work

- Cloud providers, including Google Drive, Dropbox, or WebDAV.
- Older generations and public sharing.

Jeff authorized creation of a new public GitHub repository, commits, pushes, Actions and Pages configuration, and testing the live site after local verification. Work remains on main.

## Reference

[Jeff's Scarlet Blueberry tracker](https://pokedextracker.com/u/trowgundam/scarlet-blueberry) provides the box presentation and information-panel reference.
