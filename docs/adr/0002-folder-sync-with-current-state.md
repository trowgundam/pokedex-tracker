# Use folder synchronization with one current state per tracker

The initial release synchronizes through a user-selected folder, which external software such as Syncthing can transfer between devices. Jeff chose one current state file per tracker and rejected application-managed snapshots. Keep provider storage operations separate from tracker behavior so cloud providers can be added later. Folder writes cannot assume atomic expected-version checks against external synchronization software; the conflict policy must account for that limitation.

Current filenames contain the tracker ID and a SHA-256 content hash. A save verifies a new file before removing only the acknowledged preceding files. Different concurrent versions, changed baseline tokens, corrupt content, and Syncthing conflict copies pause that tracker's writes until the user chooses. The app records edit and synchronization times but never chooses a winner by timestamp.

Jeff chose file removal for deletion rather than a folder deletion marker. Other devices explicitly accept disappearance or recreate their local tracker. Cleared local deletion records prevent late-arriving versions from being silently resurrected. No deleted checklist history is retained.

Brave may not expose the picker. The app detects this and keeps local tracking and backup import/export available. The site does not enable flags or change browser settings.

Folder sync requires Web Locks as well as secure-context directory access. Local IndexedDB saves and folder commits use one origin-wide lock, and folder commits recheck the local revision while holding it. Browsers without Web Locks retain local tracking and backups. This lock coordinates application tabs only; external synchronization tools still require file-token checks and explicit conflict resolution.
