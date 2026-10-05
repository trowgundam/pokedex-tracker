// Run against a disposable preview origin. Drive Settings and tracker navigation
// through actual Blazor DOM events; remove only the trackers created here.
async function runPokedexThemeChecks() {
    let passed = 0, minimumContrast = Infinity;
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
    const openSettings = async () => {
        click('#tracker-menu > summary');
        click('.tracker-menu-actions button:last-child');
        await wait(() => document.querySelector('#settings-dialog').open, 'Settings did not open.');
    };
    const closeSettings = async () => {
        click('#settings-dialog .dialog-actions button');
        await wait(() => !document.querySelector('#settings-dialog').open, 'Settings did not close.');
    };
    const appearance = async (theme, scheme) => {
        change('[aria-label="Theme"]', theme);
        change('[aria-label="Color scheme"]', scheme);
        await wait(() => document.documentElement.dataset.theme === theme && document.documentElement.dataset.colorScheme === scheme, 'Appearance did not change.');
    };
    const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
    const context = canvas.getContext('2d');
    const rgb = color => { context.clearRect(0, 0, 1, 1); context.fillStyle = color; context.fillRect(0, 0, 1, 1); return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3); };
    const luminance = color => rgb(color).map(v => v / 255).map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4).reduce((sum, v, i) => sum + v * [.2126, .7152, .0722][i], 0);
    const contrast = (foreground, background) => { const a = luminance(foreground), b = luminance(background); return (Math.max(a, b) + .05) / (Math.min(a, b) + .05); };
    const expectAccent = hex => check(JSON.stringify(rgb(getComputedStyle(document.querySelector('.brand-mark')).borderTopColor)) === JSON.stringify(rgb(hex)), 'The selected edition uses ' + hex + '.');
    const checkContrast = () => {
        const body = getComputedStyle(document.body), button = getComputedStyle(document.querySelector('.tracker-menu-actions .primary'));
        const muted = getComputedStyle(document.querySelector('.brand small')).color;
        const ratios = [contrast(body.color, body.backgroundColor), contrast(muted, body.backgroundColor), contrast(button.color, button.backgroundColor), contrast(button.backgroundColor, body.backgroundColor)];
        minimumContrast = Math.min(minimumContrast, ...ratios);
        check(ratios.every(value => value >= 4.5), 'Text, muted text, primary button, and accent have at least 4.5:1 contrast.');
    };
    const games = [
        ['letsgo-pikachu', '#8a6000', '#f3cc4d'], ['letsgo-eevee', '#855021', '#d6ac74'],
        ['sword', '#006e91', '#63cfec'], ['shield', '#a42658', '#ef8bab'],
        ['brilliant-diamond', '#225ec4', '#94beff'], ['shining-pearl', '#a02777', '#ed9bd3'],
        ['legends-arceus', '#176f61', '#76ccae'], ['scarlet', '#ae351f', '#ffad8e'],
        ['violet', '#7440bd', '#c5a0f4'], ['legends-za', '#087465', '#61dabb'],
        ['firered', '#ad420f', '#ffac72'], ['leafgreen', '#3b7426', '#9ed47a'],
        ['home', '#315da3', '#9abbed']
    ];
    await wait(ready, 'Startup did not complete.');
    const originalCount = document.querySelectorAll('.tracker-item').length;
    const originalTheme = document.querySelector('[aria-label="Theme"]').value;
    const originalScheme = document.querySelector('[aria-label="Color scheme"]').value;
    await openSettings();
    check(['catppuccin', 'game'].every(id => [...document.querySelector('[aria-label="Theme"]').options].some(option => option.value === id)), 'Settings separates actual themes from Auto/Light/Dark.');
    if (originalCount === 0) {
        await appearance('game', 'light'); expectAccent('#315da3'); checkContrast();
        await appearance('game', 'dark'); expectAccent('#9abbed'); checkContrast();
        check(document.documentElement.dataset.game === '', 'The welcome screen uses the generic palette.');
    }
    await closeSettings();
    const prefix = 'Theme verification ' + Date.now(), names = [];
    for (const [game, light, dark] of games) {
        const name = prefix + ' ' + game; names.push(name);
        click('#tracker-menu > summary'); click('.tracker-menu-actions .primary');
        await wait(() => document.querySelector('#create-dialog').open, 'Create did not open.');
        change('[aria-label="Game"]', game);
        change('#create-dialog input', name);
        click('#create-dialog button[type="submit"]');
        await wait(() => document.querySelector('main h1')?.textContent === name && !document.querySelector('#create-dialog').open && ready() && document.documentElement.dataset.game === game, 'The new tracker did not select its game palette.');
        await openSettings();
        await appearance('game', 'light'); expectAccent(light); checkContrast();
        await appearance('game', 'dark'); expectAccent(dark); checkContrast();
        await appearance('game', 'system'); expectAccent(matchMedia('(prefers-color-scheme: dark)').matches ? dark : light);
        await appearance('catppuccin', 'light');
        check(getComputedStyle(document.body).backgroundColor === 'rgb(239, 241, 245)', 'Catppuccin Latte ignores the game.');
        await appearance('catppuccin', 'dark');
        check(getComputedStyle(document.body).backgroundColor === 'rgb(30, 30, 46)', 'Catppuccin Mocha ignores the game.');
        await appearance('game', 'dark');
        await closeSettings();
    }
    // Switching existing trackers must update the palette without reopening Settings.
    for (const game of ['scarlet', 'violet', 'scarlet']) {
        click('#tracker-menu > summary');
        [...document.querySelectorAll('.tracker-item')].find(button => button.querySelector('strong').textContent === prefix + ' ' + game).click();
        await wait(() => document.documentElement.dataset.game === game, 'Selecting an existing tracker did not update the palette.');
        expectAccent(game === 'scarlet' ? '#ffad8e' : '#c5a0f4');
    }
    for (const name of names) {
        click('#tracker-menu > summary');
        [...document.querySelectorAll('.tracker-item')].find(button => button.querySelector('strong').textContent === name).click();
        document.querySelector('.tracker-actions').open = true;
        click('.tracker-actions button:nth-child(4)');
        await wait(() => document.querySelector('#action-dialog').open, 'Delete did not open.');
        click('#action-dialog button[type="submit"]');
        await wait(() => !document.querySelector('#action-dialog').open && ready() && ![...document.querySelectorAll('.tracker-item strong')].some(element => element.textContent === name), 'The verification tracker was not removed.');
    }
    check(document.querySelectorAll('.tracker-item').length === originalCount, 'Theme checks remove only their own trackers.');
    if (originalCount === 0) { await wait(() => document.documentElement.dataset.game === '', 'Deleting the final tracker did not restore generic colors.'); expectAccent('#9abbed'); }
    await openSettings(); await appearance(originalTheme, originalScheme); await closeSettings();
    return { passed, minimumContrast: minimumContrast.toFixed(2) + ':1', origin: location.origin };
}
runPokedexThemeChecks();
