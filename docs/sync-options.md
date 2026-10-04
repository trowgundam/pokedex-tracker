# Browser-only synchronization options

The site must remain a standalone Blazor WebAssembly application on GitHub Pages. Browsers synchronize directly with user-owned storage; the operator holds neither progress nor provider tokens. Jeff selected only a user-selected folder provider for the initial release. Store one current state file per tracker and do not implement snapshot history. Keep the interface modular for later cloud providers.

## Required conflict behavior

Each tracker has independent progress. A device retains the baseline and time of its last successful synchronization. When local edits are pending and remote progress has changed since that baseline, the user must acknowledge reconciliation before either version is replaced.

A read followed by an unconditional upload is insufficient: another device can write between those operations. A provider must reject stale replacements, or the storage format must preserve concurrent versions without overwriting them. Provider revisions identify the baseline more reliably than device clocks.

## Dropbox

Dropbox documents uploads against a specific file revision. Revision-checked updates with automatic renaming disabled and strict conflict detection can reject a stale write, including a write against a file that has since been deleted. This directly supports the requested conflict prompt and a separate JSON file per tracker.

Browser PKCE authentication works without a client secret. Dropbox recommends short-lived tokens without refresh tokens for pure browser applications, so reconnection remains part of the expected experience. A public app requires production approval as its user count grows.

Paid accounts are not required. Dropbox's developer FAQ permits API apps for free and paid accounts. Third-party API app connections do not count toward Basic's three-device limit.

Sources: [JavaScript SDK upload contract](https://dropbox.github.io/dropbox-sdk-js/global.html), [OAuth guide](https://docs.dropboxapi.com/dropbox-api/docs/oauth), [production requirements](https://docs.dropboxapi.com/dropbox-api/docs/developer-resources/developer-guide).

Account sources: [developer FAQ](https://www.dropbox.com/developers/support), [Dropbox API support clarification](https://community.dropbox.com/en/discussion/334652/three-devices-limit-and-api-access).

## Google Drive

The narrow `drive.appdata` permission stores files in the user's application-data folder. Google's browser token model needs a user-driven action to obtain a replacement token after expiration.

Drive exposes an output-only, increasing file version. Its current v3 content-update documentation does not establish an atomic expected-version condition. That is a documentation gap, not proof that conditional writes are impossible.

One possible design stores immutable snapshots with parent references. Concurrent edits preserve both snapshots and require explicit reconciliation. This avoids unconditional replacement, but adds snapshot discovery and retention work. This design has not been selected or tested.

Sources: [application-data folder](https://developers.google.com/workspace/drive/api/guides/appdata), [browser token model](https://developers.google.com/identity/oauth2/web/guides/use-token-model), [file metadata](https://developers.google.com/workspace/drive/api/reference/rest/v3/files), [file updates](https://developers.google.com/workspace/drive/api/reference/rest/v3/files/update).

## OneDrive

Microsoft supports SPA authentication with PKCE and no client secret. SPA refresh tokens expire after 24 hours, with subsequent authorization potentially requiring interaction. The current permissions reference labels delegated `Files.ReadWrite.AppFolder` as preview.

Upload-session creation documents an `If-Match` condition, but this investigation has not established stale-write rejection at the final content commit. A proof of concept must settle that before selecting a shared-file replacement design.

Sources: [SPA authentication](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow), [app-folder permissions](https://learn.microsoft.com/en-us/graph/permissions-reference#filesreadwriteappfolder), [upload sessions](https://learn.microsoft.com/en-us/graph/api/driveitem-createuploadsession).

## Self-hosted WebDAV

A browser-accessible WebDAV server can provide a FOSS storage option. Nextcloud and ownCloud are potential hosts. The user needs a server reachable by all their devices, appropriate credentials, and CORS configuration permitting requests from the tracker site's origin. Browser-trusted HTTPS is the portable default.

Nextcloud documents GET, PUT, and directory operations with app passwords. That does not establish cross-origin support in every standard installation. The WebAppPassword add-on explicitly supplies CORS and an allowed-origin configuration. App passwords are not necessarily limited to a particular folder.

Sources: [Nextcloud WebDAV API](https://docs.nextcloud.com/server/stable/developer_manual/client_apis/WebDAV/basic.html), [WebAppPassword configuration](https://github.com/digital-blueprint/webapppassword), [ownCloud WebDAV configuration](https://doc.owncloud.com/ocis/8.3/admin/deployment/services/s-list/webdav.html).

## User-selected folder and Syncthing

The folder provider uses the browser's read/write directory picker. The user grants access to an ordinary filesystem directory that Syncthing or another tool synchronizes. Current compatibility data lists support in desktop Chrome and Edge and Chrome on Android; Firefox and Safari, including iOS, do not support that picker. Detect support at runtime and handle permission renewal.

Syncthing's REST API manages its synchronization service and exposes metadata; it is not a general file-content storage API. The browser should write to the selected folder rather than use that API as a storage backend.

Syncthing can create conflict copies. The selected current-file format uses tracker IDs and content hashes in filenames. The app groups copies by tracker identity, preserves different candidates, and prompts before reconciliation. It creates no snapshot history.

The app can report that it saved files to the folder, but cannot claim that an external tool has delivered them to another device. A share already mounted by the operating system may be selectable in the picker, subject to testing. An arbitrary SMB or NFS path is not directly usable as a browser storage endpoint.

The host has Brave version 1.96.60. Its matching source disables File System Access by default and registers `brave://flags/#file-system-access-api` on desktop. Jeff reports that the flag is absent in his installation. Runtime capability detection is authoritative for the app. Do not require a flag or change browser settings; unsupported browsers retain local tracking and backups.

Source: [Brave default setting](https://github.com/brave/brave-core/blob/v1.96.60/chromium_src/third_party/blink/common/features.cc#L9), [Brave desktop flag](https://github.com/brave/brave-core/blob/v1.96.60/browser/about_flags.cc#L1277).

Sources: [Chrome filesystem guide](https://developer.chrome.com/docs/capabilities/web-apis/file-system-access), [current browser compatibility data](https://github.com/mdn/browser-compat-data/blob/main/api/Window.json), [Syncthing REST API](https://docs.syncthing.net/dev/rest.html), [Syncthing conflict handling](https://docs.syncthing.net/users/syncing.html).

## Interface direction

Keep tracker behavior and reconciliation independent of storage-provider APIs. A provider is responsible for its authentication, configuration, and storage operations. A shared synchronization format should preserve concurrent versions on providers that lack conditional replacement.

Jeff rejected snapshot history. The selected format has one current JSON file per tracker, with an edit timestamp and a SHA-256 content hash in its filename. Each device remembers the filenames and time of its last successful synchronization.

A save freezes the content and expected candidates, verifies the baseline, creates a new hash filename, rereads it, and removes only acknowledged old files whose byte hashes still match. A final scan verifies the surviving file's name and content hash. Concurrent app saves therefore produce separate candidates rather than overwriting each other's bytes. Unexpected copies survive and require a per-tracker choice. The provider reports that arbitrary external replacements cannot be checked atomically.

Jeff chose removal-based deletion. Other devices ask whether to accept a missing synchronized file or retain and recreate their local tracker. Cleared local deletion markers recognize late-arriving states without retaining a checklist or adding folder tombstones.

IndexedDB persists current local data, pending changes, baselines, and folder handles where supported. A revision check rejects stale local saves from another tab; a stale tab blocks further edits and folder operations until it reloads.
Sources: [browser writer locking](https://developer.chrome.com/blog/new-dev-trial-for-multiple-readers-and-writers/#exclusive-writer-for-filesystemwritablefilestream), [filesystem standard on external changes](https://fs.spec.whatwg.org/#file-system-entry), [Syncthing conflicts](https://docs.syncthing.net/users/syncing.html#conflicting-changes).

## Verification status

Cloud-provider findings are based on public documentation; no cloud authentication or WebDAV integration is implemented. Real IndexedDB and OPFS file operations and per-tracker conflict choices have been exercised in the local app. Native OS picker behavior in Jeff's Brave, mounted network shares, and external Syncthing delivery remain unverified. See [the verification record](verification.md).
