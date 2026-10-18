// Dev-версия service worker: пустая (офлайн-кэш только в публикации -
// при publish SDK заменяет этот файл на service-worker.published.js
// и генерирует service-worker-assets.js).
self.addEventListener('install', () => self.skipWaiting());
