// Disposable local preview only. Reinject after each full page navigation.
// setup returns a fixture; select, assert, rename, duplicate, and delete accept it.
// Use the real browser to reload, open fixture URLs, and move Back/Forward
// between stages. No stage reads or changes the private tracker store directly.
async function runPokedexNavigationChecks(stage, fixture = {}) {
    let passed = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); passed++; };
    const wait = async condition => {
        for (let i = 0; i < 160 && !condition(); i++) await new Promise(resolve => setTimeout(resolve, 50));
        if (!condition()) throw new Error('Navigation did not reach the expected state.');
    };
    const title = () => document.querySelector('main h1')?.textContent;
    const id = () => new URL(location.href).searchParams.get('tracker');
    const click = selector => document.querySelector(selector).click();
    const change = (selector, value) => { const element = document.querySelector(selector); element.value = value; element.dispatchEvent(new Event('change', { bubbles: true })); };
    await wait(() => document.querySelector('.sidebar'));
    if (document.getElementById('tracker-navigation').hidden) click('.sidebar-toggle');
    async function select(name) {
        [...document.querySelectorAll('.tracker-item')].find(button => button.querySelector('strong').textContent === name).click();
        await wait(() => title() === name && localStorage.getItem('pokedex-last-tracker') === id());
    }
    if (stage === 'setup') {
        const names = ['First link test ' + Date.now(), 'Second link test ' + Date.now()];
        const ids = [];
        for (const name of names) {
            click('.create-tracker'); await wait(() => document.getElementById('create-dialog').open);
            change('#create-dialog input', name); click('#create-dialog button[type="submit"]');
            await wait(() => !document.getElementById('create-dialog').open && title() === name && id() !== null);
            ids.push(id());
            check(/^[a-f0-9]{32}$/.test(id()), 'New trackers expose normalized persistent IDs.');
        }
        check(ids[0] !== ids[1], 'Independent trackers have distinct URLs.');
        check(localStorage.getItem('pokedex-last-tracker') === ids[1], 'Creation remembers the new active tracker.');
        return { passed, names, ids };
    }
    if (stage === 'select') {
        await select(fixture.name);
        check(id() === fixture.id, 'Selecting a tracker updates its URL.');
    } else if (stage === 'assert') {
        await wait(() => title() === fixture.name && id() === fixture.id && localStorage.getItem('pokedex-last-tracker') === fixture.id);
        check(title() === fixture.name, 'The expected tracker is active.');
        check(localStorage.getItem('pokedex-last-tracker') === fixture.id, 'The active tracker is remembered on this device.');
        check(id() === fixture.id, fixture.explicit ? 'An explicit tracker URL takes precedence.' : 'The restored tracker has a bookmarkable URL.');
    } else if (stage === 'unavailable') {
        await wait(() => title() === 'Tracker unavailable');
        check(document.querySelector('.welcome [role="status"]').textContent.includes(fixture.invalid ? 'invalid tracker ID' : 'not available on this device'), 'Invalid and missing IDs explain why their tracker cannot open.');
        check(localStorage.getItem('pokedex-last-tracker') === fixture.lastId, 'Unavailable links preserve the remembered tracker.');
        check(id() === fixture.id, 'An unavailable link is preserved for later import or sync.');
    } else if (stage === 'rename') {
        await select(fixture.name);
        document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(1)');
        await wait(() => document.getElementById('action-dialog').open);
        change('#action-dialog input', fixture.newName); click('#action-dialog button[type="submit"]');
        await wait(() => !document.getElementById('action-dialog').open && title() === fixture.newName);
        check(id() === fixture.id, 'Renaming preserves the bookmark URL.');
    } else if (stage === 'duplicate') {
        document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(2)');
        await wait(() => title() === fixture.name + ' copy' && !document.querySelector('.create-tracker').disabled);
        check(id() !== fixture.id && localStorage.getItem('pokedex-last-tracker') === id(), 'A duplicate gets its own remembered URL.');
        return { passed, id: id(), name: title() };
    } else if (stage === 'delete') {
        await select(fixture.name);
        document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(4)');
        await wait(() => document.getElementById('action-dialog').open); click('#action-dialog button[type="submit"]');
        await wait(() => !document.getElementById('action-dialog').open && ![...document.querySelectorAll('.tracker-item strong')].some(element => element.textContent === fixture.name));
        await wait(() => id() !== fixture.id);
        check(id() !== fixture.id && localStorage.getItem('pokedex-last-tracker') === id(), 'Deletion updates both the URL and remembered selection.');
    } else throw new Error('Unknown verification stage: ' + stage);
    return { passed, title: title(), id: id() };
}
