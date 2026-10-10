// Run in a disposable preview origin. Drive the real Blazor information panel.
async function runPokedexEvolutionChecks() {
    let checks = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); checks++; };
    const wait = async condition => {
        for (let i = 0; i < 160 && !condition(); i++) await new Promise(resolve => setTimeout(resolve, 50));
        if (!condition()) throw new Error('The evolution panel did not reach the expected state.');
    };
    const click = selector => document.querySelector(selector).click();
    const change = (selector, value, event = 'change') => {
        const element = document.querySelector(selector);
        element.value = value; element.dispatchEvent(new Event(event, { bubbles: true }));
    };
    const name = 'Evolution verification ' + Date.now();
    await wait(() => document.querySelector('.sidebar'));
    if (document.querySelector('#sources-panel')) { click('[aria-label="Close sources"]'); await wait(() => !document.querySelector('#sources-panel')); }
    if (document.getElementById('tracker-navigation').hidden) click('.sidebar-toggle');
    const originalCount = document.querySelectorAll('.tracker-item').length;
    async function create(game, dex, suffix) {
        click('.create-tracker'); await wait(() => document.getElementById('create-dialog').open);
        change('[aria-label="Game"]', game);
        await wait(() => [...document.querySelector('[aria-label="Pokédex"]').options].some(option => option.value === dex));
        change('[aria-label="Pokédex"]', dex); change('#create-dialog input', name + suffix);
        click('#create-dialog button[type="submit"]');
        await wait(() => !document.getElementById('create-dialog').open && document.querySelector('main h1')?.textContent === name + suffix);
        [...document.querySelectorAll('[aria-label="Tracker view"] button')].find(b => b.textContent === 'Checklist').click();
        await wait(() => document.querySelector('[aria-label="Search Pokémon"]'));
    }
    async function show(displayName) {
        change('[aria-label="Search Pokémon"]', displayName, 'input');
        await wait(() => document.querySelector(`[aria-label="Sources for ${displayName}"]`));
        click(`[aria-label="Sources for ${displayName}"]`);
        await wait(() => document.querySelector('#info-title')?.textContent === displayName);
    }
    const ids = () => [...document.querySelectorAll('[data-evolution-pokemon]')].map(e => e.dataset.evolutionPokemon);
    const panel = () => document.querySelector('#sources-panel');
    await create('scarlet', 'paldea', ' Paldea');
    await show('Floragato');
    check(JSON.stringify(ids()) === JSON.stringify(['sprigatito', 'floragato', 'meowscarada']), 'Middle-stage selection shows the entire family in order.');
    check(JSON.stringify([...panel().querySelectorAll('.evolution-requirement')].map(e => e.textContent.trim())) === JSON.stringify(['↓ Level 16', '↓ Level 36']), 'Starter requirements show the correct levels.');
    check(panel().querySelector('[aria-current="true"]').dataset.evolutionPokemon === 'floragato', 'The selected Pokémon is highlighted.');
    await wait(() => [...panel().querySelectorAll('.evolution-family img')].every(i => i.complete && i.naturalWidth > 0));
    check([...panel().querySelectorAll('.evolution-family img')].every(i => new URL(i.src).origin === location.origin), 'Evolution sprites load locally.');
    check(panel().scrollWidth <= panel().clientWidth, 'The tree fits the sources pane.');
    const progress = document.querySelector('.progress-summary').textContent;
    await show('Primeape');
    check(JSON.stringify(ids()) === JSON.stringify(['mankey', 'primeape', 'annihilape']), 'Primeape includes Annihilape in the full family.');
    check(panel().textContent.includes('Use Rage Fist 20 times') && panel().textContent.includes('then level up'), 'Primeape shows its move count and level-up requirement.');
    await show('Pawmo');
    check(ids().includes('pawmot') && panel().textContent.includes('1000 steps') && panel().textContent.includes("Let's Go"), 'Walking evolutions include their verified requirement.');
    await show('Sinistea');
    check(JSON.stringify(ids()) === JSON.stringify(['sinistea', 'polteageist']) && panel().textContent.includes('Use Cracked Pot') && panel().textContent.includes('Use Chipped Pot') && panel().textContent.includes('Antique Form'), 'Cosmetic alternatives retain their conditions without duplicating the species node.');
    await show('Rockruff');
    check(ids().length === 2 && panel().textContent.includes('Own Tempo') && panel().textContent.includes('Dusk Form'), 'Lycanroc includes its ability-dependent cosmetic alternative.');
    check(panel().scrollWidth <= panel().clientWidth, 'Multiple alternative requirements wrap within the pane.');
    await show('Eevee');
    check(ids().length === 9 && ids().filter(id => id === 'eevee').length === 1, 'Eevee has one shared root and all eight branches.');
    check(panel().querySelector('.evolution-path > li > .evolution-children').children.length === 8, 'Eevee branches share the same parent in the rendered tree.');
    check(panel().textContent.includes('Use Leaf Stone') && panel().textContent.includes('knowing a Fairy-type move'), 'Branch requirements include stones and moves.');
    check(panel().scrollWidth <= panel().clientWidth, 'Long branching requirements wrap within the pane.');
    await show('Perrserker');
    check(JSON.stringify(ids()) === JSON.stringify(['meowth-galar', 'perrserker']), 'Regional evolution shows the correct parent and excludes Persian.');
    await show('Ditto');
    check(!panel().querySelector('.evolution-family'), 'Pokémon without evolution paths have no empty section.');
    check(document.querySelector('.progress-summary').textContent === progress, 'Viewing evolution trees does not change progress.');
    click('[aria-label="Close sources"]');
    [...document.querySelectorAll('[aria-label="Tracker view"] button')].find(b => b.textContent === 'Sources').click();
    await wait(() => document.querySelector('.source-info[aria-label="Sources for Perrserker"]'));
    const opener = document.querySelector('.source-info[aria-label="Sources for Perrserker"]');
    opener.closest('details').open = true;
    const sourceCounts = document.querySelector('.source-counts').textContent;
    opener.focus(); opener.click();
    await wait(() => document.querySelector('#info-title')?.textContent === 'Perrserker');
    check(JSON.stringify(ids()) === JSON.stringify(['meowth-galar', 'perrserker']), 'The Sources view opens the same regional evolution tree.');
    check(document.querySelector('.source-counts').textContent === sourceCounts, 'Opening the tree preserves ranked source counts.');
    panel().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await wait(() => !document.querySelector('#sources-panel'));
    check(document.activeElement === opener, 'Escape closes the pane and restores the source opener focus.');
    await create('legends-arceus', 'hisui', ' Hisui');
    await show('Quilava');
    check(JSON.stringify(ids()) === JSON.stringify(['cyndaquil', 'quilava', 'typhlosion-hisui']) && panel().textContent.includes('Evolve at level 17'), 'Hisui uses its own starter level and evolved form.');
    await show('Gengar');
    check(ids().includes('haunter') && panel().textContent.includes('Use Linking Cord or trade'), 'Arceus renders its trade and item alternatives.');
    await show('Hisuian Qwilfish');
    check(ids().includes('overqwil') && panel().textContent.includes('20 times in Strong Style'), 'Arceus renders its move-style evolution.');
    click('[aria-label="Close sources"]');
    await create('home', 'national', ' National');
    await show('Quilava');
    check(panel().querySelector('[data-evolution-game="scarlet"]').textContent.includes('Level 14') && panel().querySelector('[data-evolution-game="legends-arceus"]').textContent.includes('Evolve at level 17'), 'National separates methods by game family.');
    check(new Set([...panel().querySelectorAll('.evolution-family h3')].map(e => e.textContent)).size === panel().querySelectorAll('.evolution-family').length, 'Paired editions do not repeat identical trees.');
    await show('Primeape');
    check(panel().querySelector('[data-evolution-game="scarlet"]').textContent.includes('then level up') && panel().querySelector('[data-evolution-game="legends-za"]').textContent.includes('then evolve'), 'National keeps Scarlet and Z-A move-count actions separate.');
    click('[aria-label="Close sources"]');
    for (const suffix of [' Paldea', ' Hisui', ' National']) {
        [...document.querySelectorAll('.tracker-item')].find(b => b.querySelector('strong').textContent === name + suffix).click();
        await wait(() => document.querySelector('main h1')?.textContent === name + suffix && !document.querySelector('.create-tracker').disabled);
        document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(4)');
        await wait(() => document.getElementById('action-dialog').open); click('#action-dialog button[type="submit"]');
        await wait(() => !document.getElementById('action-dialog').open && ![...document.querySelectorAll('.tracker-item strong')].some(t => t.textContent === name + suffix) && !document.querySelector('.create-tracker').disabled);
    }
    check(document.querySelectorAll('.tracker-item').length === originalCount, 'Evolution checks remove only their own trackers.');
    return { passed: checks, width: innerWidth, origin: location.origin };
}
runPokedexEvolutionChecks();
