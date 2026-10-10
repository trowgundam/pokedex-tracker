import { createServer } from 'node:http';
import { readFile, mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve, extname, join, sep } from 'node:path';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { browserSuite } from './browser-suite.mjs';

const root = resolve(process.argv[2] ?? 'artifacts/publish/wwwroot');
const basePath = process.argv[3] ?? '/pokedex-tracker/';
const group = process.argv[4] ?? 'all';
const actions = await browserSuite(group);
if (!basePath.startsWith('/') || !basePath.endsWith('/')) throw new Error('Pass a base path with leading and trailing slashes.');
const chromePath = process.env.CHROME_BIN ?? ['/usr/bin/google-chrome', '/usr/bin/google-chrome-stable', '/usr/bin/chromium', '/usr/bin/chromium-browser'].find(existsSync);
if (!chromePath) throw new Error('Set CHROME_BIN to an installed Chromium or Chrome executable.');
const profile = await mkdtemp(join(tmpdir(), 'pokedex-browser-'));
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.css': 'text/css', '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml', '.webmanifest': 'application/manifest+json' };
const server = createServer(async (request, response) => {
    try {
        const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
        if (!pathname.startsWith(basePath)) { response.writeHead(404).end(); return; }
        const file = resolve(root, pathname.slice(basePath.length) || 'index.html');
        if (!file.startsWith(root + sep)) { response.writeHead(403).end(); return; }
        const content = await readFile(file);
        response.writeHead(200, { 'Content-Type': mime[extname(file)] ?? 'application/octet-stream', 'Cache-Control': 'no-cache' });
        response.end(content);
    } catch { response.writeHead(404).end(); }
});
server.listen(0, '127.0.0.1');
await once(server, 'listening');
const origin = `http://127.0.0.1:${server.address().port}`;
const url = origin + basePath;
let chrome, cdp;
let chromeLog = '';
let currentAction = 'Start browser';

try {
    chrome = spawn(chromePath, ['--headless=new', '--no-sandbox', '--disable-dev-shm-usage', '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'], { stdio: ['ignore', 'ignore', 'pipe'] });
    let launchError;
    chrome.on('error', error => { launchError = error; });
    chrome.stderr.on('data', data => { chromeLog = (chromeLog + data).slice(-12000); });
    let port;
    for (let i = 0; i < 150; i++) {
        if (launchError) throw launchError;
        if (chrome.exitCode !== null || chrome.signalCode !== null) throw new Error(`Chrome exited during startup: ${chromeLog}`);
        try { port = Number((await readFile(join(profile, 'DevToolsActivePort'), 'utf8')).split('\n')[0]); break; } catch { await new Promise(r => setTimeout(r, 100)); }
    }
    if (!port) throw new Error(`Chrome did not start: ${chromeLog}`);
    const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    const page = targets.find(target => target.type === 'page');
    if (!page) throw new Error('Chrome did not create a browser page.');
    cdp = await connect(page.webSocketDebuggerUrl);
    await cdp.call('Page.enable');
    await cdp.call('Page.setLifecycleEventsEnabled', { enabled: true });
    await cdp.call('Runtime.enable');
    await cdp.call('Network.enable');
    const evaluate = async expression => {
        const result = await cdp.call('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
        if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? result.exceptionDetails.text);
        return result.result.value;
    };
    const navigate = async target => {
        const result = await cdp.call('Page.navigate', { url: target });
        if (result.errorText) throw new Error(result.errorText);
        await cdp.waitFor('Page.lifecycleEvent', event => event.name === 'load' && event.loaderId === result.loaderId);
        for (let i = 0; i < 600; i++) {
            try { if (await evaluate("!!document.querySelector('.sidebar')")) return; } catch { /* Navigation replaces the JavaScript context. */ }
            await new Promise(r => setTimeout(r, 100));
        }
        throw new Error('The app did not finish startup: ' + await evaluate('document.body.innerText.slice(0, 2000)'));
    };
    for (const action of actions) {
        currentAction = action.name;
        console.log(action.name);
        switch (action.kind) {
            case 'reset':
                await cdp.call('Page.navigate', { url: 'about:blank' });
                await cdp.call('Storage.clearDataForOrigin', { origin, storageTypes: 'all' });
                await navigate(url);
                break;
            case 'resize':
                await cdp.call('Emulation.setDeviceMetricsOverride', { width: action.width, height: action.height, deviceScaleFactor: 1, mobile: false });
                break;
            case 'evaluate': {
                const value = await evaluate(action.expression);
                if (value !== undefined) console.log(JSON.stringify(value));
                break;
            }
            case 'navigate':
                await navigate(action.urlExpression ? await evaluate(action.urlExpression) : url + (action.query ?? ''));
                break;
            case 'offline':
                await cdp.call('Network.emulateNetworkConditions', { offline: action.offline, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
                break;
            default: throw new Error('Unknown browser action: ' + action.kind);
        }
    }
    console.log(`PASS: browser group ${group}.`);
} catch (error) {
    console.error(`FAIL: browser group ${group}, action ${currentAction}: ${error.message}`);
    if (cdp) {
        await mkdir('artifacts', { recursive: true });
        try {
            const shot = await cdp.call('Page.captureScreenshot');
            await writeFile('artifacts/browser-failure.png', Buffer.from(shot.data, 'base64'));
            const text = await cdp.call('Runtime.evaluate', { expression: 'document.body.innerText', returnByValue: true });
            await writeFile('artifacts/browser-failure.txt', text.result.value ?? '');
        } catch { /* Keep the original test failure. */ }
    }
    throw error;
} finally {
    cdp?.close();
    if (chrome && chrome.exitCode === null && chrome.signalCode === null) {
        const stopped = once(chrome, 'exit');
        chrome.kill('SIGTERM');
        await Promise.race([stopped, new Promise(r => setTimeout(r, 5000))]);
        if (chrome.exitCode === null && chrome.signalCode === null) { chrome.kill('SIGKILL'); await stopped; }
    }
    server.closeAllConnections();
    await new Promise(r => server.close(r));
    await rm(profile, { recursive: true, force: true });
}

async function connect(url) {
    const socket = new WebSocket(url);
    await once(socket, 'open');
    let sequence = 0;
    const pending = new Map();
    const events = [];
    const waiting = new Set();
    socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        if (message.method) {
            events.push(message);
            if (events.length > 100) events.shift();
            for (const waiter of waiting) {
                if (waiter.method === message.method && waiter.matches(message.params)) {
                    waiting.delete(waiter); clearTimeout(waiter.timer); waiter.resolve();
                }
            }
        }
        const request = pending.get(message.id);
        if (!request) return;
        pending.delete(message.id);
        clearTimeout(request.timer);
        if (message.error) request.reject(new Error(message.error.message));
        else request.resolve(message.result);
    });
    socket.addEventListener('close', () => {
        for (const request of pending.values()) { clearTimeout(request.timer); request.reject(new Error('Browser connection closed.')); }
        pending.clear();
        for (const waiter of waiting) { clearTimeout(waiter.timer); waiter.reject(new Error('Browser connection closed.')); }
        waiting.clear();
    });
    return {
        call(method, params = {}) {
            return new Promise((resolve, reject) => {
                const id = ++sequence;
                const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Browser command timed out: ${method}`)); }, 90000);
                pending.set(id, { resolve, reject, timer });
                socket.send(JSON.stringify({ id, method, params }));
            });
        },
        waitFor(method, matches) {
            if (events.some(event => event.method === method && matches(event.params))) return Promise.resolve();
            return new Promise((resolve, reject) => {
                const waiter = { method, matches, resolve, reject };
                waiter.timer = setTimeout(() => { waiting.delete(waiter); reject(new Error('Browser event timed out: ' + method)); }, 60000);
                waiting.add(waiter);
            });
        },
        close() { socket.close(); }
    };
}
