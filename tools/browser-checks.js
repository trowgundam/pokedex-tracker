// Run in a disposable local preview origin's console. Uses real IndexedDB and OPFS,
// with only the operating-system directory picker replaced for unattended testing.
async function runPokedexBrowserChecks() {
    const app = await import(new URL('app.js', document.baseURI));
    const root = await navigator.storage.getDirectory();
    const testName = 'pokedex-checks-' + crypto.randomUUID();
    const folder = await root.getDirectoryHandle(testName, { create: true });
    const originalPicker = window.showDirectoryPicker;
    const id = '12345678123412341234123456789abc';
    let checks = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); checks++; };
    const hash = async content => [...new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(content)))].map(x => x.toString(16).padStart(2, '0')).join('');
    const filename = async content => `tracker-${id}-${await hash(content)}.json`;
    const write = async (name, content) => {
        const writer = await (await folder.getFileHandle(name, { create: true })).createWritable();
        await writer.write(content); await writer.close();
    };
    const rejects = async (action, message) => {
        let rejected = false;
        try { await action(); } catch { rejected = true; }
        check(rejected, message);
    };
    try {
        window.showDirectoryPicker = async () => folder;
        await app.loadLocal();
        check(await app.chooseFolder() === testName, 'Folder handle is selected and stored in IndexedDB.');
        const locksDescriptor = Object.getOwnPropertyDescriptor(navigator, 'locks');
        try {
            Object.defineProperty(navigator, 'locks', { configurable: true, value: undefined });
            check(!app.capabilities().folder, 'Folder support is disabled without Web Locks even when a picker is available.');
            await rejects(() => app.chooseFolder(), 'Folder selection requires Web Locks.');
            await rejects(() => app.reconnectFolder(), 'Remembered folders cannot reconnect without Web Locks.');
            await rejects(() => app.listFolder(), 'Folder reads are blocked without Web Locks.');
            await rejects(() => app.commitFolder(id, [], null, null), 'Folder mutations are blocked without Web Locks.');
            await app.saveLocal(await app.loadLocal());
            check(typeof await app.loadLocal() === 'string', 'Local persistence still works without Web Locks.');
        } finally {
            if (locksDescriptor) Object.defineProperty(navigator, 'locks', locksDescriptor);
            else delete navigator.locks;
        }
        const a = JSON.stringify({ id: '12345678-1234-1234-1234-123456789abc', name: 'A', checked: [] });
        const b = JSON.stringify({ id: '12345678-1234-1234-1234-123456789abc', name: 'B', checked: ['pikachu'] });
        const c = JSON.stringify({ id: '12345678-1234-1234-1234-123456789abc', name: 'C', checked: ['eevee'] });
        let files = await app.commitFolder(id, [], await filename(a), a);
        check(files.length === 1 && files[0].content === a && files[0].integrityValid, 'A real current file is written and its hash verified.');
        files = await app.commitFolder(id, files, await filename(b), b);
        check(files.length === 1 && files[0].content === b, 'Replacement removes the previous current file.');
        const baseline = files;
        await write(await filename(c), c);
        await rejects(async () => app.commitFolder(id, baseline, await filename(a), a), 'A concurrent version blocks stale replacement.');
        files = await app.listFolder();
        check(files.length === 2 && files.some(f => f.content === b) && files.some(f => f.content === c), 'Both conflicting versions survive.');
        const copyName = (await filename(c)).replace('.json', '.sync-conflict-20261004-device.json');
        await write(copyName, c);
        files = await app.listFolder();
        check(files.some(f => f.fileName === copyName && f.filenameId === id && f.integrityValid), 'Syncthing copies keep their tracker identity and hash.');
        files = await app.commitFolder(id, files, await filename(c), c);
        check(files.length === 1 && files[0].content === c, 'An acknowledged resolution produces one current file.');
        await write(await filename(c), a);
        files = await app.listFolder();
        check(files.length === 1 && !files[0].integrityValid, 'External content edits trigger an integrity warning.');
        const corruptBaseline = files;
        await write(await filename(c), b);
        await rejects(async () => app.commitFolder(id, corruptBaseline, await filename(c), c), 'A corrupt file changed after acknowledgement is preserved.');
        check((await app.listFolder())[0].content === b, 'Rejected recovery leaves the newer corrupt contents intact.');
        files = await app.listFolder();
        files = await app.commitFolder(id, files, await filename(c), c);
        check(files.length === 1 && files[0].content === c && files[0].integrityValid, 'Acknowledged local recovery restores its own corrupt hash target.');
        await write(await filename(c), a);
        files = await app.listFolder();
        files = await app.commitFolder(id, files, await filename(b), b);
        check(files.length === 1 && files[0].content === b, 'Explicit replacement of acknowledged corruption is verified.');
        const otherTab = await import(new URL('app.js?verification=' + crypto.randomUUID(), document.baseURI));
        await otherTab.chooseFolder();
        const localContent = await otherTab.loadLocal();
        await app.validateLocal();
        await otherTab.saveLocal(localContent);
        await rejects(async () => app.commitFolder(id, files, await filename(c), c), 'A tab invalidated after validation cannot mutate the folder.');
        check((await app.listFolder())[0].content === b, 'A stale tab leaves the folder state intact.');
        await app.loadLocal();
        await otherTab.loadLocal();
        const originalGetFile = FileSystemFileHandle.prototype.getFile;
        let entered, release;
        const readEntered = new Promise(resolve => { entered = resolve; });
        const resumeRead = new Promise(resolve => { release = resolve; });
        let pause = true, saveFinished = false;
        FileSystemFileHandle.prototype.getFile = async function () {
            if (pause && this.name === files[0].fileName) { pause = false; entered(); await resumeRead; }
            return originalGetFile.call(this);
        };
        let commit, save;
        try {
            commit = app.commitFolder(id, files, await filename(c), c);
            await readEntered;
            save = otherTab.saveLocal(localContent).then(() => { saveFinished = true; });
            await new Promise(resolve => setTimeout(resolve, 50));
            check(!saveFinished, 'A local save in another tab waits while the folder commit holds the shared lock.');
        } finally {
            release();
            FileSystemFileHandle.prototype.getFile = originalGetFile;
            if (commit) files = await commit;
            if (save) await save;
        }
        check(saveFinished && files[0].content === c, 'The folder commit finishes before the queued local save.');
        await app.loadLocal();
        files = await app.commitFolder(id, files, null, null);
        check(files.length === 0, 'Deletion removes the current file without creating a snapshot or tombstone.');
        check((await app.commitFolder(id, [], null, null)).length === 0, 'Retrying a completed deletion is idempotent.');
        return { passed: checks, storage: 'Real IndexedDB and OPFS', picker: 'Replaced with an isolated test directory' };
    } finally {
        window.showDirectoryPicker = originalPicker;
        await app.forgetFolder();
        await root.removeEntry(testName, { recursive: true });
    }
}
runPokedexBrowserChecks();
