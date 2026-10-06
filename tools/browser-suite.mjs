import { readFile } from 'node:fs/promises';

// The same plan drives CI's disposable browser and the collaborative preview.
export async function browserSuite() {
    const script = name => readFile(new URL(name, import.meta.url), 'utf8');
    const actions = [];
    const add = (name, kind, options = {}) => actions.push({ name, kind, ...options });
    for (const [file, width] of [['browser-ui-checks.js', 1440], ['browser-ui-checks.js', 390], ['browser-catalog-checks.js', 1440], ['browser-theme-checks.js', 1440], ['browser-source-checks.js', 1440], ['browser-sync-checks.js', 1440], ['browser-storage-checks.js', 1440], ['browser-checks.js', 1440]]) {
        add(`Reset for ${file} at ${width}px`, 'reset');
        add(`Resize to ${width}px`, 'resize', { width, height: 900 });
        add(`${file} at ${width}px`, 'evaluate', { expression: (await script(file)).trim() });
    }
    const navigation = await script('browser-navigation-checks.js');
    const fixture = "JSON.parse(sessionStorage.getItem('checks-navigation'))";
    const check = (stage, args) => `${navigation}\nrunPokedexNavigationChecks(${JSON.stringify(stage)}, ${args})`;
    add('Reset for navigation', 'reset');
    add('Create navigation fixtures', 'evaluate', { expression: `${navigation}\n(async () => { const fixture = await runPokedexNavigationChecks('setup'); sessionStorage.setItem('checks-navigation', JSON.stringify(fixture)); return fixture; })()` });
    add('Reload remembers the last tracker', 'navigate');
    add('Verify remembered selection', 'evaluate', { expression: check('assert', `{name: ${fixture}.names[1], id: ${fixture}.ids[1]}`) });
    for (const index of [0, 1]) add(`Select tracker ${index + 1}`, 'evaluate', { expression: check('select', `{name: ${fixture}.names[${index}], id: ${fixture}.ids[${index}]}`) });
    add('Browser Back', 'evaluate', { expression: 'history.back()' });
    add('Verify Back', 'evaluate', { expression: check('assert', `{name: ${fixture}.names[0], id: ${fixture}.ids[0], explicit: true}`) });
    add('Browser Forward', 'evaluate', { expression: 'history.forward()' });
    add('Verify Forward', 'evaluate', { expression: check('assert', `{name: ${fixture}.names[1], id: ${fixture}.ids[1], explicit: true}`) });
    add('Open a bookmark', 'navigate', { urlExpression: `new URL('?tracker=' + ${fixture}.ids[0], document.baseURI).href` });
    add('Verify bookmark', 'evaluate', { expression: check('assert', `{name: ${fixture}.names[0], id: ${fixture}.ids[0], explicit: true}`) });
    add('Open an invalid tracker URL', 'navigate', { query: '?tracker=invalid' });
    add('Verify invalid link', 'evaluate', { expression: check('unavailable', `{id: 'invalid', invalid: true, lastId: ${fixture}.ids[0]}`) });
    add('Open a missing tracker URL', 'navigate', { query: '?tracker=00000000000000000000000000000001' });
    add('Verify missing link', 'evaluate', { expression: check('unavailable', `{id: '00000000000000000000000000000001', lastId: ${fixture}.ids[0]}`) });
    add('Return to the remembered tracker', 'navigate');
    add('Rename preserves the URL', 'evaluate', { expression: check('rename', `{name: ${fixture}.names[0], id: ${fixture}.ids[0], newName: ${fixture}.names[0] + ' renamed'}`) });
    add('Duplicate gets a new URL', 'evaluate', { expression: `${navigation}\n(async () => { const copy = await runPokedexNavigationChecks('duplicate', {name: ${fixture}.names[0] + ' renamed', id: ${fixture}.ids[0]}); sessionStorage.setItem('checks-copy', JSON.stringify(copy)); return copy; })()` });
    add('Delete the duplicate', 'evaluate', { expression: check('delete', "JSON.parse(sessionStorage.getItem('checks-copy'))") });
    add('Delete the renamed tracker', 'evaluate', { expression: check('delete', `{name: ${fixture}.names[0] + ' renamed', id: ${fixture}.ids[0]}`) });
    add('Delete the second tracker', 'evaluate', { expression: check('delete', `{name: ${fixture}.names[1], id: ${fixture}.ids[1]}`) });
    add('Reset for offline reopening', 'reset');
    add('Wait for the release offline cache', 'evaluate', { expression: `(async () => { await navigator.serviceWorker.ready; for (let i = 0; i < 600 && !navigator.serviceWorker.controller; i++) await new Promise(r => setTimeout(r, 100)); if (!navigator.serviceWorker.controller) throw new Error('The release service worker did not take control.'); return {cached: true}; })()` });
    add('Create offline fixtures', 'evaluate', { expression: `${navigation}\n(async () => {
        const fixture = await runPokedexNavigationChecks('setup');
        sessionStorage.setItem('checks-navigation', JSON.stringify(fixture));
        document.querySelector('[aria-label="Mark Sprigatito"]').click();
        const saved = () => new Promise((resolve, reject) => {
            const open = indexedDB.open('pokedex-tracker');
            open.onerror = () => reject(open.error);
            open.onsuccess = () => {
                const db = open.result;
                const read = db.transaction('settings').objectStore('settings').get('trackers');
                read.onsuccess = () => {
                    db.close();
                    resolve(JSON.parse(read.result?.content ?? '[]').some(t => t.state.id.replaceAll('-', '') === fixture.ids[1] && t.state.checked.includes('sprigatito')));
                };
                read.onerror = () => { db.close(); reject(read.error); };
            };
        });
        for (let i = 0; i < 160; i++) {
            if (await saved()) return {checked: true};
            await new Promise(r => setTimeout(r, 50));
        }
        throw new Error('Offline fixture was not persisted.');
    })()` });
    add('Disconnect the browser network', 'offline', { offline: true });
    add('Reopen offline', 'navigate');
    add('Verify offline progress', 'evaluate', { expression: `${navigation}\n(async () => { const result = await runPokedexNavigationChecks('assert', {name: ${fixture}.names[1], id: ${fixture}.ids[1]}); if (!document.querySelector('[aria-label="Mark Sprigatito"]').checked) throw new Error('Offline progress was lost.'); return {...result, checked: true}; })()` });
    add('Restore the browser network', 'offline', { offline: false });
    return actions;
}

if (process.argv[1] === new URL(import.meta.url).pathname) console.log(JSON.stringify(await browserSuite()));
