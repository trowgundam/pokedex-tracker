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
    const click = selector => { const element = document.querySelector(selector); element.focus(); element.click(); };
    const change = (selector, value) => {
        const input = document.querySelector(selector);
        input.value = value;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    };
    const ready = () => !!document.querySelector('.sidebar') && !document.querySelector('.create-tracker')?.disabled;
    const expandSidebar = async () => { if (document.getElementById('tracker-navigation').hidden) { click('.sidebar-toggle'); await wait(() => !document.getElementById('tracker-navigation').hidden, 'Sidebar did not expand.'); } };
    const openSettings = async () => {
        await expandSidebar();
        click('.settings-button');
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
    const expectAccent = hex => check(JSON.stringify(rgb(getComputedStyle(document.querySelector('.create-tracker')).backgroundColor)) === JSON.stringify(rgb(hex)), 'The selected edition uses ' + hex + '.');
    const checkContrast = () => {
        const body = getComputedStyle(document.body), button = getComputedStyle(document.querySelector('.create-tracker'));
        const muted = getComputedStyle(document.querySelector('.menu-empty') || document.querySelector('.tracker-item span')).color;
        const dialog = getComputedStyle(document.querySelector('#settings-dialog'));
        const accentText = getComputedStyle(document.querySelector('#settings-dialog .text-button')).color;
        const ratios = [contrast(body.color, body.backgroundColor), contrast(muted, body.backgroundColor), contrast(button.color, button.backgroundColor), contrast(accentText, dialog.backgroundColor)];
        minimumContrast = Math.min(minimumContrast, ...ratios);
        check(ratios.every(value => value >= 4.5), 'Text, muted text, primary-button labels, and accent text have at least 4.5:1 contrast.');
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
    const originalAccent = localStorage.getItem('pokedex-accent-catppuccin') ?? 'blue';
    await openSettings();
    check(['catppuccin', 'normal'].every(id => [...document.querySelector('[aria-label="Theme"]').options].some(option => option.value === id)), 'Settings separates actual themes from Auto/Light/Dark.');
    if (originalCount === 0) {
        await appearance('normal', 'light'); expectAccent('#315da3'); checkContrast();
        await appearance('normal', 'dark'); expectAccent('#9abbed'); checkContrast();
        check(document.documentElement.dataset.game === '', 'The welcome screen uses the generic palette.');
    }
    await closeSettings();
    const prefix = 'Theme verification ' + Date.now(), names = [];
    for (const [game, light, dark] of games) {
        const name = prefix + ' ' + game; names.push(name);
        await expandSidebar(); click('.create-tracker');
        await wait(() => document.querySelector('#create-dialog').open, 'Create did not open.');
        change('[aria-label="Game"]', game);
        change('#create-dialog input', name);
        click('#create-dialog button[type="submit"]');
        await wait(() => document.querySelector('main h1')?.textContent === name && !document.querySelector('#create-dialog').open && ready() && document.documentElement.dataset.game === game, 'The new tracker did not select its game palette.');
        await openSettings();
        await appearance('normal', 'light'); expectAccent(light); checkContrast();
        check(getComputedStyle(document.body).backgroundColor === 'rgb(255, 255, 255)' && !document.querySelector('[aria-label="Accent color"]'), 'Normal light surfaces stay neutral and accent follows the game automatically.');
        await appearance('normal', 'dark'); expectAccent(dark); checkContrast();
        check(getComputedStyle(document.body).backgroundColor === 'rgb(24, 24, 27)', 'Normal dark surfaces stay neutral across games.');
        await appearance('normal', 'system'); expectAccent(matchMedia('(prefers-color-scheme: dark)').matches ? dark : light);
        await appearance('catppuccin', 'light');
        check(getComputedStyle(document.body).backgroundColor === 'rgb(239, 241, 245)', 'Catppuccin Latte ignores the game.');
        await appearance('catppuccin', 'dark');
        check(getComputedStyle(document.body).backgroundColor === 'rgb(30, 30, 46)', 'Catppuccin Mocha ignores the game.');
        await appearance('normal', 'dark');
        await closeSettings();
    }
    const accents = [
        ['rosewater', '#dc8a78', '#f5e0dc'], ['flamingo', '#dd7878', '#f2cdcd'],
        ['pink', '#ea76cb', '#f5c2e7'], ['mauve', '#8839ef', '#cba6f7'],
        ['red', '#d20f39', '#f38ba8'], ['maroon', '#e64553', '#eba0ac'],
        ['peach', '#fe640b', '#fab387'], ['yellow', '#df8e1d', '#f9e2af'],
        ['green', '#40a02b', '#a6e3a1'], ['teal', '#179299', '#94e2d5'],
        ['sky', '#04a5e5', '#89dceb'], ['sapphire', '#209fb5', '#74c7ec'],
        ['blue', '#1e66f5', '#89b4fa'], ['lavender', '#7287fd', '#b4befe']
    ];
    await openSettings();
    await appearance('catppuccin', 'light');
    check(document.querySelector('[aria-label="Accent color"]').options.length === 14, 'Catppuccin offers all 14 accents.');
    for (const [accent, light, dark] of accents) {
        change('[aria-label="Accent color"]', accent);
        await wait(() => document.documentElement.dataset.accent === accent, 'Accent did not update.');
        await appearance('catppuccin', 'light'); expectAccent(light); checkContrast();
        await appearance('catppuccin', 'dark'); expectAccent(dark); checkContrast();
        await appearance('catppuccin', 'system'); expectAccent(matchMedia('(prefers-color-scheme: dark)').matches ? dark : light);
    }
    await appearance('normal', 'dark');
    await appearance('catppuccin', 'dark');
    check(document.querySelector('[aria-label="Accent color"]').value === 'lavender', 'Switching themes remembers the chosen Catppuccin accent.');
    change('[aria-label="Accent color"]', originalAccent);
    await wait(() => document.documentElement.dataset.accent === originalAccent, 'The original accent did not restore.');
    await appearance('normal', 'dark');
    await closeSettings();
    // Switching existing trackers must update the palette without reopening Settings.
    for (const game of ['scarlet', 'violet', 'scarlet']) {
        await expandSidebar();
        [...document.querySelectorAll('.tracker-item')].find(button => button.querySelector('strong').textContent === prefix + ' ' + game).click();
        await wait(() => document.documentElement.dataset.game === game, 'Selecting an existing tracker did not update the palette.');
        expectAccent(game === 'scarlet' ? '#ffad8e' : '#c5a0f4');
    }
    for (const name of names) {
        await expandSidebar();
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
