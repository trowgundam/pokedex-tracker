// Run only on a disposable local origin with no trackers. Uses the real app,
// IndexedDB and OPFS; only the operating-system folder picker is substituted.
async function runPokedexSyncChecks() {
    const wait = async (condition, message) => {
        for (let attempt = 0; attempt < 160 && !condition(); attempt++)
            await new Promise(resolve => setTimeout(resolve, 50));
        if (!condition()) throw new Error(message);
    };
    const click = selector => document.querySelector(selector).click();
    const change = (selector, value) => {
        const element = document.querySelector(selector);
        element.value = value;
        element.dispatchEvent(new Event('change', { bubbles: true }));
    };
    const button = text => [...document.querySelectorAll('button')].find(element => element.textContent.trim() === text);
    const idle = () => !document.querySelector('.create-tracker')?.disabled;
    let checks = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); checks++; };
    await wait(() => document.querySelector('.sidebar') && idle(), 'App did not finish loading.');
    if (document.querySelectorAll('.tracker-item').length !== 0)
        throw new Error('Use an empty disposable origin for sync checks.');
    const app = await import(new URL('app.js', document.baseURI));
    const root = await navigator.storage.getDirectory();
    const name = 'pokedex-sync-checks-' + crypto.randomUUID();
    const folder = await root.getDirectoryHandle(name, { create: true });
    const originalPicker = window.showDirectoryPicker;
    try {
        window.showDirectoryPicker = async () => folder;
        if (document.getElementById('tracker-navigation').hidden) click('.sidebar-toggle');
        click('.create-tracker');
        await wait(() => document.getElementById('create-dialog').open, 'Create dialog did not open.');
        change('[aria-label="Game"]', 'scarlet');
        await wait(() => document.querySelector('[aria-label="Pokédex"]').value === 'paldea', 'Paldea was not selected.');
        change('#create-dialog input', name);
        click('#create-dialog button[type="submit"]');
        await wait(() => !document.getElementById('create-dialog').open && idle(), 'Creation did not finish.');
        click('[aria-label="Mark Sprigatito"]');
        await wait(() => document.querySelector('[aria-label="Mark Sprigatito"]').checked && idle(), 'Check was not saved.');
        click('.settings-button');
        await wait(() => document.getElementById('settings-dialog').open, 'Settings did not open.');
        button('Choose folder').click();
        await wait(() => button('Sync now'), 'Test folder did not connect.');
        button('Sync now').click();
        await wait(() => document.querySelector('.save-status').textContent.startsWith('Folder synchronized') && idle(), 'Initial sync failed.');
        const saved = (await app.listFolder())[0];
        check(saved.integrityValid && JSON.parse(saved.content).checked.includes('sprigatito'), 'The real tracker is saved with its check and a valid hash.');
        const beforeSync = JSON.parse(await app.loadLocal()).find(tracker => !tracker.deleted);
        const beforeModified = (await (await folder.getFileHandle(saved.fileName)).getFile()).lastModified;
        await new Promise(resolve => setTimeout(resolve, 25));
        button('Sync now').click();
        let afterSync;
        for (let attempt = 0; attempt < 160; attempt++) {
            afterSync = JSON.parse(await app.loadLocal()).find(tracker => !tracker.deleted);
            if (afterSync.lastSyncedUtc !== beforeSync.lastSyncedUtc && idle()) break;
            await new Promise(resolve => setTimeout(resolve, 50));
        }
        check(Date.parse(afterSync.lastSyncedUtc) > Date.parse(beforeSync.lastSyncedUtc), 'An unchanged successful sync refreshes the saved synchronization time.');
        check(afterSync.state.lastEditedUtc === beforeSync.state.lastEditedUtc &&
            (await (await folder.getFileHandle(saved.fileName)).getFile()).lastModified === beforeModified,
            'Unchanged sync updates only local sync metadata, leaving the folder file and edit time untouched.');
        const corrupt = JSON.stringify({ ...JSON.parse(saved.content), name: 'Externally changed', checked: [] });
        const writer = await (await folder.getFileHandle(saved.fileName)).createWritable();
        await writer.write(corrupt); await writer.close();
        button('Sync now').click();
        await wait(() => document.querySelector('.conflict-panel') && idle(), 'Corruption did not produce a conflict.');
        button('Review conflicts').click();
        await wait(() => !document.getElementById('settings-dialog').open, 'Settings did not close.');
        check(document.querySelector('[aria-label="Mark Sprigatito"]').checked, 'Corruption does not silently discard the local check.');
        button("Keep this device's version").click();
        await wait(() => !document.querySelector('.conflict-panel') && idle(), 'Acknowledged local recovery failed.');
        const recovered = await app.listFolder();
        check(recovered.length === 1 && recovered[0].fileName === saved.fileName && recovered[0].content === saved.content && recovered[0].integrityValid,
            'Keep-local recovery restores the exact current file and its hash.');
        check(document.querySelector('[aria-label="Mark Sprigatito"]').checked && !document.querySelector('.notice.error'), 'The local checklist survives recovery without an error.');
        document.querySelector('.tracker-actions').open = true;
        button('Delete tracker').click();
        await wait(() => document.getElementById('action-dialog').open, 'Delete dialog did not open.');
        click('#action-dialog button[type="submit"]');
        await wait(() => !document.getElementById('action-dialog').open && idle(), 'Test tracker was not deleted.');
        click('.settings-button');
        await wait(() => document.getElementById('settings-dialog').open, 'Settings did not reopen.');
        button('Sync now').click();
        await wait(() => document.querySelector('.save-status').textContent.startsWith('Folder synchronized') && idle(), 'Deletion sync failed.');
        check((await app.listFolder()).length === 0 && document.querySelectorAll('.tracker-item').length === 0, 'The test tracker and folder file are removed through the app.');
        button('Disconnect folder').click();
        await wait(() => !button('Sync now'), 'Folder did not disconnect.');
        click('#settings-dialog .dialog-actions button');
        return { passed: checks, events: 'Actual Blazor DOM events', storage: 'Real IndexedDB and OPFS' };
    } finally {
        window.showDirectoryPicker = originalPicker;
        await app.forgetFolder();
        await root.removeEntry(name, { recursive: true });
    }
}
runPokedexSyncChecks();
