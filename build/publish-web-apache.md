# Публикация Lines.Web на Apache 2.4 (виртуальный хост lines.lan)

Игра — статический сайт (Blazor WebAssembly + KNI BlazorGL), серверный код
не требуется. Ниже — настройка отдельного виртуального хоста `lines.lan`.

## 1. Сборка релиза

```
powershell -ExecutionPolicy Bypass -File LinesMonoGame/build/publish-web.ps1
```

Результат: `LinesMonoGame/build/out/web/wwwroot` — копируем целиком
(включая скрытый `.htaccess`) в каталог сайта, например
`C:\Apache24\htdocs\lines.lan\`.

## 2. Файл hosts

В `C:\Windows\System32\drivers\etc\hosts` (от имени администратора):

```
127.0.0.1   lines.lan
```

## 3. Виртуальный хост Apache

В `conf/extra/httpd-vhosts.conf`:

```apache
<VirtualHost *:80>
    ServerName lines.lan
    DocumentRoot "C:/Apache24/htdocs/lines.lan"

    <Directory "C:/Apache24/htdocs/lines.lan">
        Options Indexes FollowSymLinks
        AllowOverride All
        Require all granted

        # MIME-типы Blazor WebAssembly (без application/wasm игра
        # не загрузится: «streaming compile failed»)
        AddType application/wasm .wasm
        AddType application/octet-stream .dll .dat
        AddType application/json .json .blat
    </Directory>

    ErrorLog "logs/lines.lan-error.log"
    CustomLog "logs/lines.lan-access.log" common
</VirtualHost>
```

В `conf/httpd.conf` должны быть подключены виртуальные хосты и MIME-модуль
(раскомментировать, если закомментировано):

```apache
Include conf/extra/httpd-vhosts.conf
LoadModule mime_module modules/mod_mime.so
```

Замечания:

- `AddType` продублированы и в `.htaccess` внутри публикации — при
  `AllowOverride All` достаточно любого из двух мест.
- SPA-fallback не нужен: игра живёт на одной странице (`index.html`),
  `<base href="./">` позволяет работать и в корне хоста, и в подпапке.
- Предсжатые `.br` не используются (в `index.html`
  `enableBrotliDecompression = false`), mod_brotli/mod_deflate
  настраивать не требуется.

## 4. Перезапуск и проверка

```
httpd -t            # проверка синтаксиса конфигов
httpd -k restart    # перезапуск Apache
```

Открыть http://lines.lan/ — должна появиться заставка «Шарики».
Если вместо игры ошибка — на странице отображается текст исключения
(блок `#jsError` вверху), детали также в консоли браузера (F12).

## 5. Обновление версии

Повторить шаг 1 и скопировать файлы поверх. Обновление подхватывается
service worker'ом: при каждой публикации SDK подставляет в начало
`service-worker.js` новую версию (`/* Manifest version: ... */`),
браузер видит изменённый файл и переключается на новый кэш. Чтобы это
работало сразу, `.htaccess` отдаёт `service-worker*.js`, `index.html`
и `manifest.webmanifest` с `Cache-Control: no-cache`.

ВНИМАНИЕ: в первой версии `.htaccess` `service-worker.js` кэшировался
на сутки - клиенты, заходившие на сайт с тем `.htaccess`, могут до 24
часов получать старый кэш. Разовое решение на клиенте: DevTools (F12)
→ Application → Service Workers → Unregister и Storage → Clear site
data, затем обычная перезагрузка.

## 6. Доступ из локальной сети

1. **Apache слушает все интерфейсы**: в `httpd.conf` должно быть
   `Listen 80` (без привязки к `127.0.0.1`); vhost `<VirtualHost *:80>`
   уже подходит.
2. **Брандмауэр Windows** - открыть входящий TCP 80 (PowerShell от
   администратора):
   ```powershell
   New-NetFirewallRule -DisplayName "Apache HTTP (lines.lan)" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
   ```
3. **Имя lines.lan на клиентах** - должно резолвиться в LAN-IP сервера
   (`ipconfig` на сервере, например 192.168.1.50). Варианты:
   - hosts каждого клиента: `192.168.1.50   lines.lan`
     (`C:\Windows\System32\drivers\etc\hosts`, на Linux/macOS - `/etc/hosts`);
   - статическая DNS-запись на роутере (`lines.lan` → IP сервера) +
     привязка IP к MAC сервера (DHCP reservation), чтобы адрес не менялся;
   - свой DNS-сервер (dnsmasq, Technitium) для большой сети.

### Technitium DNS Server (выбранный вариант)

1. Админ-панель: `http://localhost:5380/` на сервере, при первом входе
   задать пароль администратора. Служба «Technitium DNS Server» должна
   стартовать автоматически.
2. Zones → Add Zone → Primary Zone, имя `lines.lan`; в зоне - запись
   **A** с именем `@` и значением = LAN-IP сервера с Apache.
3. Привязать IP сервера к MAC на роутере (DHCP reservation), иначе
   запись протухнет при смене адреса.
4. Раздача DNS клиентам: в DHCP роутера указать DNS-сервером IP машины
   с Technitium (тогда все устройства, включая iPhone, получат его
   автоматически). Если роутер не позволяет - на iPhone: Настройки →
   Wi-Fi → (i) у сети → Настройка DNS → «Вручную» → IP сервера.
   Остальные запросы Technitium разрешает сам (встроенная рекурсия)
   или через форвардеры в Settings.
5. Брандмауэр сервера: открыть входящие UDP и TCP 53:
   ```powershell
   New-NetFirewallRule -DisplayName "Technitium DNS" -Direction Inbound -Protocol UDP -LocalPort 53 -Action Allow
   New-NetFirewallRule -DisplayName "Technitium DNS" -Direction Inbound -Protocol TCP -LocalPort 53 -Action Allow
   ```
6. Проверка: `nslookup lines.lan` с клиента → IP сервера; Safari на
   iPhone → `http://lines.lan/` → заставка «Шарики».
4. Проверка без DNS: если vhost `lines.lan` первый в
   `httpd-vhosts.conf`, Apache обслуживает его как хост по умолчанию -
   с клиента работает и `http://<LAN-IP-сервера>/`.
5. Суффикс `.local` конфликтует с mDNS (Bonjour) на macOS/Linux/iOS -
   поэтому используется `lines.lan` (на iPhone/iPad `.local` вообще не
   резолвится через DNS, а hosts там не редактируется).

## 7. Доступ с iPhone/iPad (iOS)

1. DNS: запись `lines.lan → <LAN-IP сервера>` на роутере (статический
   DNS) или свой DNS-сервер (Technitium) - hosts на iOS недоступен.
   Быстрая проверка связи без DNS: `http://<LAN-IP сервера>/` в Safari.
2. Для HTTPS/PWA на iPhone нужен корневой CA mkcert:
   - `mkcert -CAROOT` на сервере → файл `rootCA.pem` передать на iPhone
     (почта/AirDrop/мессенджер) и открыть;
   - Настройки → Основные → VPN и управление устройством → установить
     профиль mkcert;
   - ОБЯЗАТЕЛЬНО: Настройки → Основные → Об этом устройстве → Доверие
     сертификатам → включить mkcert (иначе Safari не доверяет сайту).
3. Открыть `https://lines.lan/` в Safari.
4. Установка как приложение: «Поделиться» → «На экран "Домой"» -
   игра работает standalone и офлайн (service worker, iOS 11.3+).

## 7. PWA и офлайн-режим (HTTPS обязателен)

Публикация содержит PWA-манифест и service worker: после первого визита
игра кэшируется целиком и работает без сервера; в браузере доступна
установка как приложения («Установить» / «Add to home screen»).

Ограничение браузеров: service worker и установка PWA работают только
в secure context - **HTTPS или localhost**. По `http://lines.lan/`
офлайн-режим не включится (сама игра при этом работает). Настройка TLS
для lines.lan:

1. Сгенерировать локальный сертификат через mkcert
   (https://github.com/FiloSottile/mkcert):
   ```
   choco install mkcert   # или скачать релиз с GitHub
   mkcert -install        # локальный CA в доверенные корни
   mkcert lines.lan     # -> lines.lan.pem + lines.lan-key.pem
   ```
   Файлы положить, например, в `C:\Apache24\conf\ssl\`.
   Чтобы сертификат доверялся на других устройствах сети, на каждое
   устройство установить корневой CA mkcert (`mkcert -CAROOT` показывает
   путь к `rootCA.pem`).
2. В `httpd.conf` подключить:
   ```apache
   LoadModule ssl_module modules/mod_ssl.so
   LoadModule socache_shmcb_module modules/mod_socache_shmcb.so
   Listen 443
   ```
3. В `httpd-vhosts.conf` добавить:
   ```apache
   <VirtualHost *:443>
       ServerName lines.lan
       DocumentRoot "C:/Apache24/htdocs/lines.lan"

       SSLEngine on
       SSLCertificateFile "conf/ssl/lines.lan.pem"
       SSLCertificateKeyFile "conf/ssl/lines.lan-key.pem"

       <Directory "C:/Apache24/htdocs/lines.lan">
           AllowOverride All
           Require all granted
           AddType application/wasm .wasm
           AddType application/octet-stream .dll .dat
           AddType application/json .json .blat
       </Directory>
   </VirtualHost>
   ```
4. `httpd -t` + `httpd -k restart`, открыть https://lines.lan/ .
   После первой загрузки игра работает офлайн (кэш service worker)
   и устанавливается как приложение.
