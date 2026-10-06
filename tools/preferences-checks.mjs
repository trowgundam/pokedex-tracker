import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
import { test } from 'node:test';

const source = await readFile(new URL('../pokedex_tracker/wwwroot/app.js', import.meta.url), 'utf8');
const app = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);
globalThis.document = { documentElement: { dataset: {} } };

test('blocked preference reads use the default appearance', () => {
    globalThis.localStorage = { getItem() { throw new DOMException('Denied', 'SecurityError'); } };
    assert.equal(app.getColorScheme(), 'system');
    assert.equal(app.getThemeChoice(), 'catppuccin');
    assert.equal(app.getThemeAccent('catppuccin'), null);
});

test('failed preference writes keep the session appearance and report failure', () => {
    globalThis.localStorage = { setItem() { throw new DOMException('Full', 'QuotaExceededError'); } };
    assert.equal(app.setThemeChoice('normal'), false);
    assert.equal(app.setColorScheme('dark'), false);
    assert.equal(app.setThemeAccent('catppuccin', 'mauve'), false);
    assert.equal(document.documentElement.dataset.theme, 'normal');
    assert.equal(document.documentElement.dataset.colorScheme, 'dark');
});

test('saved preferences round trip and preserve the previous game theme', () => {
    const values = new Map([['pokedex-appearance', 'game']]);
    globalThis.localStorage = { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value) };
    assert.equal(app.getThemeChoice(), 'normal');
    assert.equal(app.setThemeChoice('catppuccin'), true);
    assert.equal(app.setColorScheme('light'), true);
    assert.equal(app.setThemeAccent('catppuccin', 'mauve'), true);
    assert.equal(app.getThemeChoice(), 'catppuccin');
    assert.equal(app.getColorScheme(), 'light');
    assert.equal(app.getThemeAccent('catppuccin'), 'mauve');
});
