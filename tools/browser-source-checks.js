// Run against a disposable local preview. Exercises the rendered Blazor app.
async function runPokedexSourceChecks() {
    let checks = 0;
    const check = (condition, message) => { if (!condition) throw new Error(message); checks++; };
    const wait = async condition => {
        for (let i = 0; i < 160 && !condition(); i++) await new Promise(resolve => setTimeout(resolve, 50));
        if (!condition()) throw new Error('The Sources view did not reach the expected state.');
    };
    const click = selector => document.querySelector(selector).click();
    const change = (selector, value) => { const element = document.querySelector(selector); element.value = value; element.dispatchEvent(new Event('change', { bubbles: true })); };
    const view = sources => [...document.querySelectorAll('[aria-label="Tracker view"] button')].find(button => button.textContent === (sources ? 'Sources' : 'Checklist')).click();
    const count = area => Number(document.querySelector(`.source-location[data-area="${area}"] .location-count`).textContent.split(' ')[0]);
    const name = 'Source verification ' + Date.now();
    await wait(() => document.querySelector('.sidebar'));
    const originalCount = document.querySelectorAll('.tracker-item').length;
    if (document.getElementById('tracker-navigation').hidden) click('.sidebar-toggle');
    async function create(game, dex, suffix) {
        click('.create-tracker'); await wait(() => document.getElementById('create-dialog').open);
        change('[aria-label="Game"]', game);
        await wait(() => [...document.querySelector('[aria-label="Pokédex"]').options].some(option => option.value === dex));
        change('[aria-label="Pokédex"]', dex); change('#create-dialog input', name + suffix);
        click('#create-dialog button[type="submit"]');
        await wait(() => !document.getElementById('create-dialog').open && document.querySelector('main h1')?.textContent === name + suffix);
    }
    await create('scarlet', 'blueberry', ' Blueberry'); view(true);
    await wait(() => document.querySelector('.source-location'));
    check(document.querySelector('.location-title').textContent === 'Coastal Biome' && count('Coastal Biome') === 69 && count('Canyon Biome') === 64, 'Blueberry locations rank by outstanding Pokémon count.');
    check([...document.querySelectorAll('.location-list .source-location')].every(row => row.dataset.game === 'scarlet'), 'Only the selected edition contributes locations.');
    check(document.querySelector('.unlocated-sources .location-count').textContent === '74 unchecked' && document.querySelector('.unlocated-sources [data-pokemon="rhyperior"]'), 'Entries without locations remain accessible separately.');
    const coastal = document.querySelector('[data-area="Coastal Biome"]'); coastal.open = true;
    coastal.querySelector('[data-pokemon="deerling"] label').click();
    await wait(() => count('Coastal Biome') === 68 && count('Canyon Biome') === 63 && !document.querySelector('.create-tracker').disabled);
    check(!document.querySelector('.source-pokemon-list [data-pokemon="deerling"]'), 'Checking an overlapping Pokémon removes it from every source.');
    check(document.querySelector('[data-area="Coastal Biome"]').open, 'An expanded location stays open after counts update.');
    const first = document.querySelector('[data-area="Coastal Biome"] .source-info');
    const pokemonName = first.getAttribute('aria-label').slice('Sources for '.length);
    const before = document.querySelector('.source-counts').textContent;
    first.focus(); first.click(); await wait(() => document.querySelector('#info-title')?.textContent === pokemonName);
    check(document.querySelector('.source-counts').textContent === before, 'The information panel does not change progress.');
    click('[aria-label="Close sources"]'); await wait(() => !document.querySelector('#sources-panel'));
    view(false); await wait(() => document.querySelector('[aria-label="Mark Deerling"]') && !document.querySelector('[aria-label="Mark Deerling"]').disabled);
    check(document.querySelector('[aria-label="Mark Deerling"]').checked, 'Source checks appear in the box checklist.');
    click('[aria-label="Mark Deerling"]'); await wait(() => document.querySelector('.progress-summary strong').textContent.trim().startsWith('0 ') && !document.querySelector('.create-tracker').disabled);
    view(true); await wait(() => count('Coastal Biome') === 69);
    check(count('Canyon Biome') === 64, 'Unchecking in the checklist restores all source counts.');
    await create('home', 'national', ' National');
    await wait(() => document.querySelector('.source-overview'));
    check(document.querySelector('.source-counts').textContent.includes('games'), 'National sources report games.');
    const games = [...document.querySelectorAll('.location-list .source-location')];
    check(games.length === 12 && new Set(games.map(row => row.dataset.game)).size === 12 && games.every(row => !row.dataset.area), 'National groups each supported edition once without route details.');
    const counts = games.map(row => Number(row.querySelector('.location-count').textContent.split(' ')[0]));
    check(counts.every((value, index) => !index || counts[index - 1] >= value), 'National games rank by outstanding Pokémon.');
    const scarlet = document.querySelector('.source-location[data-game="scarlet"]'); scarlet.open = true;
    const initial = Number(scarlet.querySelector('.location-count').textContent.split(' ')[0]);
    scarlet.querySelector('[data-pokemon="perrserker"] label').click();
    await wait(() => Number(document.querySelector('.source-location[data-game="scarlet"] .location-count').textContent.split(' ')[0]) === initial - 1 && !document.querySelector('.create-tracker').disabled);
    check(!document.querySelector('.source-pokemon-list [data-pokemon="perrserker"]'), 'National counts include evolution-only species and update across games.');
    for (const suffix of [' Blueberry', ' National']) {
        [...document.querySelectorAll('.tracker-item')].find(button => button.querySelector('strong').textContent === name + suffix).click();
        await wait(() => document.querySelector('main h1')?.textContent === name + suffix && !document.querySelector('.create-tracker').disabled);
        document.querySelector('.tracker-actions').open = true; click('.tracker-actions button:nth-child(4)');
        await wait(() => document.getElementById('action-dialog').open); click('#action-dialog button[type="submit"]');
        await wait(() => !document.getElementById('action-dialog').open && ![...document.querySelectorAll('.tracker-item strong')].some(title => title.textContent === name + suffix) && !document.querySelector('.create-tracker').disabled);
    }
    check(document.querySelectorAll('.tracker-item').length === originalCount, 'Source tests remove only their own trackers.');
    return { passed: checks, origin: location.origin };
}
runPokedexSourceChecks();
