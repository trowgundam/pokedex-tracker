// Disposable local origin only. Aborts a real IndexedDB save through the actual UI.
async function runPokedexStorageChecks() {
    let passed = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); passed++; };
    const wait = async condition => {
        for (let i = 0; i < 160 && !condition(); i++) await new Promise(r => setTimeout(r, 50));
        if (!condition()) throw new Error('Storage controls did not reach the expected state.');
    };
    const click = selector => document.querySelector(selector).click();
    const change = (selector, value) => { const el = document.querySelector(selector); el.value = value; el.dispatchEvent(new Event('change', { bubbles: true })); };
    const idle = () => !document.querySelector('.create-tracker').disabled;
    const name = ('Storage verification ' + Date.now()).padEnd(120, 'A');
    await wait(() => !!document.querySelector('.sidebar'));
    if (document.getElementById('tracker-navigation').hidden) click('.sidebar-toggle');
    check(document.querySelectorAll('.tracker-item').length === 0, 'Storage checks start with an empty disposable origin.');
    click('.create-tracker'); await wait(() => document.getElementById('create-dialog').open);
    change('#create-dialog input', '   '); click('#create-dialog button[type="submit"]');
    await wait(() => !!document.querySelector('#create-dialog [role="alert"]') && idle());
    check(document.querySelector('#create-dialog [role="alert"]').textContent.includes('between 1 and 120'), 'Creation errors appear inside the active dialog.');
    change('#create-dialog input', name); click('#create-dialog button[type="submit"]');
    await wait(() => !document.getElementById('create-dialog').open && idle());
    click('[aria-label="Mark Sprigatito"]'); await wait(() => document.querySelector('[aria-label="Mark Sprigatito"]').checked && idle());
    const originalPut = IDBObjectStore.prototype.put;
    let aborted = false;
    try {
        IDBObjectStore.prototype.put = function (value, key) {
            if (key === 'trackers') { aborted = true; this.transaction.abort(); return; }
            return originalPut.call(this, value, key);
        };
        click('[aria-label="Mark Fuecoco"]');
        await wait(() => !!document.querySelector('main .notice.error') && idle());
    } finally { IDBObjectStore.prototype.put = originalPut; }
    check(aborted && !document.querySelector('[aria-label="Mark Fuecoco"]').checked && document.querySelector('[aria-label="Mark Sprigatito"]').checked, 'An aborted save reverts the failed edit and preserves the preceding check.');
    check(document.querySelector('main .notice.error').textContent.includes('The last edit was reverted.') && !document.querySelector('main .notice.error').textContent.includes('export'), 'Failed-edit advice matches the actual rollback.');
    const app = await import(new URL('app.js', document.baseURI));
    check(JSON.parse(await app.loadLocal()).find(t => !t.deleted).state.checked.join(',') === 'sprigatito', 'Real IndexedDB retains only the successfully saved check.');
    document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(2)');
    const copyName = name.slice(0, 115) + ' copy';
    await wait(() => document.querySelector('main h1')?.textContent === copyName && idle());
    check(document.querySelector('main h1').textContent.length === 120, 'A duplicate of the longest valid name stays within the name limit.');
    document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(1)');
    await wait(() => document.getElementById('action-dialog').open); click('#action-dialog button[type="submit"]');
    await wait(() => !document.getElementById('action-dialog').open && idle());
    check(document.querySelector('main h1').textContent === copyName, 'The generated duplicate name can be resubmitted unchanged.');
    for (const title of [copyName, name]) {
        [...document.querySelectorAll('.tracker-item')].find(el => el.querySelector('strong').textContent === title).click();
        await wait(() => document.querySelector('main h1')?.textContent === title && idle());
        document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(4)');
        await wait(() => document.getElementById('action-dialog').open); click('#action-dialog button[type="submit"]');
        await wait(() => !document.getElementById('action-dialog').open && idle());
    }
    check(document.querySelectorAll('.tracker-item').length === 0, 'Storage verification removes its own trackers.');
    return { passed, storage: 'Real aborted IndexedDB transaction' };
}
runPokedexStorageChecks();
