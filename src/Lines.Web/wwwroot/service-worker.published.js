// PWA service worker: офлайн-кэш всех ассетов игры (_framework, WASM,
// _content, Content/*.xnb). Список файлов с хэшами генерируется при
// публикации в service-worker-assets.js (ServiceWorkerAssetsManifest
// в Lines.Web.csproj). На localhost (разработка) кэш выключен.
try { importScripts('service-worker-assets.js'); } catch { /* dev: нет манифеста */ }

const isDevHost = ['localhost', '127.0.0.1'].includes(self.location.hostname);
const assetsManifest = self.assetsManifest;

if (!isDevHost && assetsManifest) {

    // версия кэша привязана к версии манифеста ассетов - при обновлении
    // публикации старый кэш автоматически удаляется в activate
    const cacheName = 'lines-cache-' + assetsManifest.version;
    const base = self.location.pathname.replace(/service-worker\.js$/, '');

    self.addEventListener('install', event => event.waitUntil(onInstall(event)));
    self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
    self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

    async function onInstall(event) {
        // все ассеты с контрольными хэшами (integrity обязателен для .dll)
        const requests = assetsManifest.assets
            .map(asset => new Request(base + asset.url, { integrity: asset.hash, cache: 'no-cache' }));
        const cache = await caches.open(cacheName);
        await cache.addAll(requests);
        self.skipWaiting();
    }

    async function onActivate(event) {
        // удаляем кэши прошлых версий
        const keys = await caches.keys();
        await Promise.all(keys.filter(key => key !== cacheName).map(key => caches.delete(key)));
        self.clients.claim();
    }

    async function onFetch(event) {
        if (event.request.method !== 'GET')
            return fetch(event.request);

        // навигация (открытие /, ./, подпапки) - всегда index.html из кэша
        if (event.request.mode === 'navigate') {
            const cachedIndex = await caches.match(base + 'index.html');
            if (cachedIndex)
                return cachedIndex;
        }

        // cache-first: всё закэшировано при установке
        const cached = await caches.match(event.request, { ignoreSearch: true });
        if (cached)
            return cached;

        return fetch(event.request);
    }
}
