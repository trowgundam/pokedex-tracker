# Initial implementation plan

This document describes the initial implementation of the [confirmed requirements](requirements.md). The implementation is complete and undergoing release verification. Jeff accepted capability-dependent Brave support and the external-write limitation.

## Application structure

Use the existing .NET 10 standalone Blazor WebAssembly starter on `main`. Separate catalog data, tracker state, synchronization, storage providers, and Razor presentation. Keep business logic in C#; use small JavaScript interop functions for browser storage, filesystem handles, and browser-only capabilities.

The storage interface lists tracker files, reads their content and an opaque comparison token, and attempts writes or deletions against an expected token. Provider implementations own authentication and storage-specific operations. The interface states whether conditional replacement is atomic; folder storage must report that it is not. Tracker code does not reference filesystem handles or cloud SDK types.

Use one current JSON file per tracker, containing its identity, name, chosen Pokédex, format version, and checked Pokémon identities. Keep no application-managed snapshot history. Browser storage preserves current local progress and pending changes independently of folder availability.

Current filenames include the tracker ID and the SHA-256 of the exact UTF-8 content. Write and verify the replacement, then remove only acknowledged preceding filenames after checking their byte hashes. Preserve unexpected concurrent files and prompt per tracker. Keep only cleared local deletion markers to recognize reappearing states; remove the folder file without a tombstone, as Jeff chose.

## Catalog

Ship a static catalog covering the supported games, DLC Pokédexes, combined lists, and National Pokédex. Generate it reproducibly from cached upstream data and reviewed project records. Encode native regional slots, regional extras, distinct regional evolutions, and the Basculin exception explicitly.

Keep stable Pokémon identities separate from display order so corrections preserve checks. Acquisition records contain route or area names by supported game and links to Serebii. Additional generations can be added through catalog records rather than game-specific UI branches.

## Synchronization

The initial provider is a user-selected folder. Remember its handle where supported and request permission again when required. External tools such as Syncthing handle transfer between devices. Report local persistence and folder persistence separately; neither establishes delivery to another device.

Remember the last successfully synchronized content token and time. If local edits and changed folder data coexist, preserve both candidates and prompt per tracker. Offer using local state or folder state; any additional reconciliation must remain explicit. Check again before a confirmed replacement and verify the write afterwards.

Detect external conflict copies by tracker identity instead of importing them as additional trackers. Pause that tracker's automatic folder writes while unresolved. Exclusive browser writers prevent some overlapping browser writes but do not provide distributed locking.

## Interface and offline behavior

Provide all agreed tracker controls. Desktop and tablet layouts follow game box geometry, using HOME geometry for National and games without ordinary boxes. Phone layouts use lists with box group headers. Extra regional entries occupy separate final boxes.

Use Catppuccin Mocha and Latte with System, Dark, and Light choices. System follows browser preference changes. Use semantic buttons, keyboard access, separate information actions, and touch-sized targets.

Cache the application, catalog, and required images so users can reopen it offline after an initial successful visit. Keep progress in persistent browser storage. Prepare static publication for a GitHub Pages repository path without relying on a server-side route fallback.

## Verification

Check catalog membership, ordering, deduplication, and known regional-form examples with a rerunnable catalog validator. Verify tracker independence, stable checks after corrections, and offline changes through actual application behavior.

Exercise selected-folder writes, reconnecting handles, importing folder changes, per-tracker conflicts, and recognition of external conflict copies. Test desktop and phone presentations, all theme modes, and offline reopening of the published application. Use Jeff's Arch KDE VM for UI tests, subject to its existing tool availability.

Jeff authorized creating a new public GitHub repository and publishing through Actions and Pages after local checks. Browser settings and cloud registrations remain outside this implementation.
