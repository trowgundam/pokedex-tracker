const databaseName = 'pokedex-tracker';
let directory;
let localRevision = 0;
const filenamePattern = /^tracker-([a-f0-9]{32})-([a-f0-9]{64})(?:[.-].*)?\.json$/i;

function database() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(databaseName, 1);
        request.onupgradeneeded = () => request.result.createObjectStore('settings');
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}
async function readSetting(key) {
    const db = await database();
    try {
        return await new Promise((resolve, reject) => {
            const request = db.transaction('settings').objectStore('settings').get(key);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    } finally { db.close(); }
}
async function writeSetting(key, value) {
    const db = await database();
    try {
        await new Promise((resolve, reject) => {
            const transaction = db.transaction('settings', 'readwrite');
            transaction.objectStore('settings').put(value, key);
            transaction.oncomplete = resolve;
            transaction.onabort = transaction.onerror = () => reject(transaction.error ?? new Error('Storage transaction failed.'));
        });
    } finally { db.close(); }
}
export async function loadLocal() {
    const value = await readSetting('trackers');
    localRevision = value?.revision ?? 0;
    return value?.content ?? '[]';
}
export async function saveLocal(content) {
    const db = await database();
    try {
        await new Promise((resolve, reject) => {
            const transaction = db.transaction('settings', 'readwrite');
            const store = transaction.objectStore('settings');
            const request = store.get('trackers');
            let conflict = false;
            request.onsuccess = () => {
                if ((request.result?.revision ?? 0) !== localRevision) {
                    conflict = true;
                    transaction.abort();
                } else store.put({ revision: localRevision + 1, content }, 'trackers');
            };
            transaction.oncomplete = () => { localRevision++; resolve(); };
            transaction.onabort = transaction.onerror = () => reject(new Error(conflict
                ? 'Another tab changed your trackers. Reload this tab before editing to protect those changes.'
                : 'Could not save on this device. Your changes are still visible; export a backup before closing.'));
        });
    } finally { db.close(); }
}
export async function validateLocal() {
    if (((await readSetting('trackers'))?.revision ?? 0) !== localRevision)
        throw new Error('Another tab changed your trackers. Reload this tab before editing to protect those changes.');
}
export function capabilities() {
    return { folder: typeof window.showDirectoryPicker === 'function' && window.isSecureContext,
        online: navigator.onLine, persistent: !!navigator.storage?.persist };
}
export async function chooseFolder() {
    const next = await window.showDirectoryPicker({ mode: 'readwrite', id: 'pokedex-trackers' });
    await writeSetting('folder', next);
    directory = next;
    return directory.name;
}
export async function reconnectFolder() {
    directory ??= await readSetting('folder');
    if (!directory) return null;
    if (await directory.queryPermission({ mode: 'readwrite' }) !== 'granted' &&
        await directory.requestPermission({ mode: 'readwrite' }) !== 'granted')
        throw new Error('Folder access was not granted. Your local progress is still saved.');
    return directory.name;
}
export async function forgetFolder() {
    await writeSetting('folder', null);
    directory = undefined;
}
async function digest(bytes) {
    const hash = await crypto.subtle.digest('SHA-256', bytes);
    return [...new Uint8Array(hash)].map(value => value.toString(16).padStart(2, '0')).join('');
}
async function readFile(name, handle) {
    const bytes = await (await handle.getFile()).arrayBuffer();
    const hash = await digest(bytes);
    const match = filenamePattern.exec(name);
    let content = '', error;
    try { content = new TextDecoder('utf-8', { fatal: true }).decode(bytes); }
    catch { error = 'The file is not valid UTF-8 text.'; }
    return { fileName: name, content, hash, error,
        integrityValid: !!match && match[2].toLowerCase() === hash, filenameId: match?.[1].toLowerCase() ?? null };
}
export async function listFolder() {
    if (!directory) throw new Error('Connect a folder first.');
    const result = [];
    for await (const [name, handle] of directory.entries()) {
        if (handle.kind !== 'file' || !name.startsWith('tracker-') || !name.endsWith('.json')) continue;
        try { result.push(await readFile(name, handle)); }
        catch (error) {
            const match = filenamePattern.exec(name);
            result.push({ fileName: name, content: '', hash: '', integrityValid: false,
                filenameId: match?.[1].toLowerCase() ?? null, error: error.message });
        }
    }
    return result;
}
function matching(files, id) {
    return files.filter(file => file.filenameId ? file.filenameId === id : (() => {
        try { return JSON.parse(file.content).id?.replaceAll('-', '').toLowerCase() === id; }
        catch { return false; }
    })());
}
function tokens(files) {
    return files.map(file => `${file.fileName}:${file.hash}`).sort().join('|');
}
export async function commitFolder(id, expected, name, content) {
    if (!directory) throw new Error('Connect a folder first.');
    const perform = async () => {
        let current = matching(await listFolder(), id);
        if (tokens(current) !== tokens(expected)) throw new Error('The folder changed during synchronization. Sync again to review the conflict.');
        if (name) {
            const bytes = new TextEncoder().encode(content);
            const hash = await digest(bytes);
            if (filenamePattern.exec(name)?.[2] !== hash) throw new Error('The proposed filename hash is incorrect.');
            let existing;
            try { existing = await directory.getFileHandle(name); }
            catch (error) { if (error.name !== 'NotFoundError') throw error; }
            if (existing) {
                if ((await readFile(name, existing)).hash !== hash) throw new Error('An existing file has a mismatched hash. It was preserved.');
            } else {
                const handle = await directory.getFileHandle(name, { create: true });
                const writer = await handle.createWritable({ mode: 'exclusive' });
                try { await writer.write(bytes); await writer.close(); }
                catch (error) { await writer.abort().catch(() => {}); throw error; }
                if ((await readFile(name, handle)).hash !== hash) throw new Error('The folder write could not be verified. Existing files were preserved.');
            }
        }
        current = matching(await listFolder(), id);
        const allowed = [...expected];
        if (name && !allowed.some(file => file.fileName === name))
            allowed.push({ fileName: name, hash: await digest(new TextEncoder().encode(content)) });
        if (tokens(current) !== tokens(allowed)) throw new Error('Another version arrived during the save. Both versions were preserved; sync again.');
        for (const file of expected) {
            if (file.fileName === name) continue;
            let handle;
            try { handle = await directory.getFileHandle(file.fileName); }
            catch (error) { if (error.name === 'NotFoundError') continue; throw error; }
            if ((await readFile(file.fileName, handle)).hash !== file.hash)
                throw new Error('A file changed before cleanup. It was preserved; sync again.');
            await directory.removeEntry(file.fileName);
        }
        const remaining = matching(await listFolder(), id);
        if (remaining.length !== (name ? 1 : 0) || name &&
            (remaining[0].fileName !== name || !remaining[0].integrityValid || remaining[0].hash !== await digest(new TextEncoder().encode(content))))
            throw new Error('Another version arrived during cleanup. Sync again to review it.');
        return remaining;
    };
    // Serializes tabs on this origin, not external folder synchronization tools.
    return navigator.locks ? navigator.locks.request('pokedex-folder-sync', perform) : perform();
}
export function setColorScheme(scheme) {
    document.documentElement.dataset.colorScheme = scheme;
    // Retain the original key so existing Auto/Light/Dark preferences survive.
    localStorage.setItem('pokedex-theme', scheme);
}
export function getColorScheme() {
    const stored = localStorage.getItem('pokedex-theme');
    return ['system', 'light', 'dark'].includes(stored) ? stored : 'system';
}
export function setThemeChoice(theme) {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('pokedex-appearance', theme);
}
export function getThemeChoice() {
    const theme = localStorage.getItem('pokedex-appearance');
    return theme === 'game' ? 'normal' : theme ?? 'catppuccin';
}
export function getThemeAccent(theme) { return localStorage.getItem('pokedex-accent-' + theme); }
export function setThemeAccent(theme, accent) { localStorage.setItem('pokedex-accent-' + theme, accent); }
export function applyAppearance(theme, scheme, gameId, accent) {
    const root = document.documentElement;
    root.dataset.theme = theme;
    root.dataset.colorScheme = scheme;
    root.dataset.game = gameId ?? '';
    root.dataset.accent = accent;
}
export async function persistStorage() { return navigator.storage?.persist ? await navigator.storage.persist() : false; }
export function download(name, content) {
    const url = URL.createObjectURL(new Blob([content], { type: 'application/json' }));
    const anchor = document.createElement('a');
    anchor.href = url; anchor.download = name; anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}
export function openDialog(id) {
    const dialog = document.getElementById(id);
    if (!dialog) return;
    const opener = document.activeElement;
    const focusTarget = opener?.closest('#tracker-menu')?.querySelector('summary') ?? opener;
    dialog.addEventListener('close', () => { if (focusTarget?.isConnected) focusTarget.focus(); }, { once: true });
    dialog.showModal();
}
export function closeDialog(id) { document.getElementById(id)?.close(); }
