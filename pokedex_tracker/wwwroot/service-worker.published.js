self.importScripts('./service-worker-assets.js');
const cacheName = 'pokedex-' + self.assetsManifest.version;
const base = new URL('./', self.location.href);
const assets = self.assetsManifest.assets.filter(asset => !asset.url.endsWith('.br') && !asset.url.endsWith('.gz'));
self.addEventListener('install', event => event.waitUntil((async () => {
    const cache = await caches.open(cacheName);
    await cache.addAll(assets.map(asset => new Request(new URL(asset.url, base), { integrity: asset.hash, cache: 'no-cache' })));
    await self.skipWaiting();
})()));
self.addEventListener('activate', event => event.waitUntil((async () => {
    await Promise.all((await caches.keys()).filter(key => key.startsWith('pokedex-') && key !== cacheName).map(key => caches.delete(key)));
    await self.clients.claim();
})()));
self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || new URL(request.url).origin !== base.origin || !new URL(request.url).pathname.startsWith(base.pathname)) return;
    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        const lookup = request.mode === 'navigate' ? new URL('index.html', base).href : request;
        return await cache.match(lookup) || fetch(request);
    })());
});
