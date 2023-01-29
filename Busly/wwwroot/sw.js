var cacheName = 'Busly';
var filesToCache = [
  '/',
  'css/site.css',
  'dist/jquery/jquery-3.6.3.js',
  'js/site.js',
  'dist/leaflet/leaflet.js',
  'dist/leaflet/leaflet.css',
  'dist/bootstrap/js/bootstrap.min.js',
  'dist/bootstrap/css/bootstrap.min.css',
  'fonts/material-icons.ttf',
  'fonts/material-icons.woff2',
  'fonts/unbounded.woff2',
  'img/bus.png',
  'img/marker-icon-2x-green.png',
  'img/marker-shadow.png'
];

/* Start the service worker and cache all of the app's content */
self.addEventListener('install', function(e) {
  e.waitUntil(
    caches.open(cacheName).then(function(cache) {
      return cache.addAll(filesToCache);
    })
  );
});

/* Serve cached content when offline */
self.addEventListener('fetch', function(e) {
  e.respondWith(
    caches.match(e.request).then(function(response) {
      return response || fetch(e.request);
    })
  );
});