// Run in a disposable preview origin. Check the catalog through actual tracker
// creation and rendered checkboxes, then remove only this script's trackers.
async function runPokedexCatalogChecks() {
    let passed = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); passed++; };
    const wait = async (condition, message) => {
        const start = Date.now();
        while (!condition()) {
            if (Date.now() - start > 8000) throw new Error(message);
            await new Promise(resolve => setTimeout(resolve, 50));
        }
    };
    const click = selector => document.querySelector(selector).click();
    const change = (selector, value) => {
        const input = document.querySelector(selector);
        input.value = value;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    };
    const ready = () => !!document.querySelector('#tracker-menu') && !document.querySelector('.tracker-menu-actions .primary')?.disabled;
    const has = name => !!document.querySelector(`[aria-label="Mark ${name}"]`);
    const cases = [
        { game: 'scarlet', dex: 'paldea', count: 404, extras: ['Galarian Meowth', 'Perrserker', 'Wooper', 'Quagsire'], source: ['Galarian Meowth', 'Naranja Academy'] },
        { game: 'scarlet', dex: 'kitakami', count: 203, extras: ['Hisuian Growlithe', 'Hisuian Arcanine', 'Tauros'], source: ['Tauros', 'Breed Paldean Tauros in Kitakami'] },
        { game: 'scarlet', dex: 'blueberry', count: 246, extras: ['Alolan Exeggutor', 'Alolan Meowth', 'Alolan Persian'], source: ['Alolan Meowth', 'League Club Room'] },
        { game: 'scarlet', dex: 'sv-complete', count: 689, extras: ['Galarian Meowth', 'Perrserker', 'Hisuian Growlithe', 'Hisuian Arcanine', 'Alolan Exeggutor', 'Alolan Meowth', 'Alolan Persian'] },
        { game: 'sword', dex: 'galar', count: 404, extras: ['Meowth', 'Galarian Slowpoke', 'Mr. Mime', 'Yamask'] },
        { game: 'sword', dex: 'isle-of-armor', count: 258, present: ['Slowking', 'Alolan Persian', "Sirfetch’d"], absent: ['Galarian Slowking', 'Galarian Moltres'] },
        { game: 'sword', dex: 'crown-tundra', count: 251, present: ['Alolan Raichu', 'Alolan Marowak'], absent: ['Alolan Exeggutor', 'Galarian Slowbro', 'Ponyta', 'Darumaka'] },
        { game: 'letsgo-pikachu', dex: 'letsgo-kanto', count: 169, present: ['Mew', 'Alolan Meowth'], absent: ['Meltan', 'Melmetal'] },
        { game: 'brilliant-diamond', dex: 'bdsp-national', count: 491, present: ['Manaphy', 'Phione'], absent: ['Celebi', 'Deoxys'] },
        { game: 'firered', dex: 'frlg-national', count: 215, present: ['Lugia', 'Ho-Oh', 'Deoxys'], absent: ['Mew', 'Celebi', 'Mareep', 'Treecko'] },
        { game: 'legends-za', dex: 'lumiose', count: 237, extras: ['Alolan Raichu', 'Galarian Slowpoke', 'Galarian Slowbro', 'Galarian Slowking', 'Galarian Stunfisk'] },
        { game: 'home', dex: 'national', count: 1083, present: ['Mew', 'Celebi', 'Deoxys', 'Meltan', 'Melmetal', 'Alolan Exeggutor'] }
    ];
    await wait(ready, 'App startup did not complete.');
    const originalCount = document.querySelectorAll('.tracker-item').length;
    const prefix = 'Catalog verification ' + Date.now();
    for (const item of cases) {
        const name = prefix + ' ' + item.dex;
        click('#tracker-menu > summary');
        click('.tracker-menu-actions .primary');
        await wait(() => document.querySelector('#create-dialog').open, 'Create dialog did not open.');
        change('[aria-label="Game"]', item.game);
        await wait(() => [...document.querySelector('[aria-label="Pokédex"]').options].some(option => option.value === item.dex), 'Game lists did not update.');
        change('[aria-label="Pokédex"]', item.dex);
        const option = document.querySelector('[aria-label="Pokédex"]').selectedOptions[0];
        check(option.textContent.endsWith(`(${item.count} entries)`), item.dex + ' offers the native count.');
        change('#create-dialog input', name);
        click('#create-dialog button[type="submit"]');
        await wait(() => document.querySelector('main h1')?.textContent === name && !document.querySelector('#create-dialog').open && ready(), 'Tracker creation did not complete.');
        check(document.querySelectorAll('.pokemon-check input').length === item.count && document.querySelector('progress').max === item.count, item.dex + ' renders exactly its native checklist.');
        if (item.extras) {
            const extras = [...document.querySelectorAll('.pokemon-box[aria-label^="Extra forms"] input')].map(input => input.getAttribute('aria-label').slice(5));
            check(JSON.stringify(extras) === JSON.stringify(item.extras), item.dex + ' renders the exact regional extras.');
        }
        if (item.present) check(item.present.every(has), item.dex + ' retains obtainable entries.');
        if (item.absent) check(item.absent.every(name => !has(name)), item.dex + ' excludes transfer-only or other-DLC entries.');
        if (item.source) {
            click(`[aria-label="Sources for ${item.source[0]}"]`);
            await wait(() => document.querySelector('#info-dialog').open, 'Sources did not open.');
            check(document.querySelector('#info-dialog').innerText.includes(item.source[1]) && !/transfer/i.test(document.querySelector('#info-dialog').innerText), item.dex + ' shows its native source.');
            click('#info-dialog button');
            await wait(() => !document.querySelector('#info-dialog').open, 'Sources did not close.');
        }
        document.querySelector('.tracker-actions').open = true;
        click('.tracker-actions button:nth-child(4)');
        await wait(() => document.querySelector('#action-dialog').open, 'Delete dialog did not open.');
        click('#action-dialog button[type="submit"]');
        await wait(() => !document.querySelector('#action-dialog').open && ready() && ![...document.querySelectorAll('.tracker-item strong')].some(element => element.textContent === name), 'Verification tracker was not removed.');
    }
    check(document.querySelectorAll('.tracker-item').length === originalCount, 'Catalog checks remove only their own trackers.');
    return { passed, origin: location.origin, events: 'Actual Blazor DOM events' };
}
runPokedexCatalogChecks();
