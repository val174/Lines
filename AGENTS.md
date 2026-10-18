# LinesMonoGame — кроссплатформенная игра «Шарики» (Lines)

Кроссплатформенная версия игры «Линии» на MonoGame.
Порт классического WinForms-проекта (`../Lines` + `../LogicLines`).

## Целевые платформы

- **Windows** (DesktopGL, OpenGL) — основная платформа разработки
- **Android** — вторая целевая платформа
- **Web** (Blazor WebAssembly + WebGL, движок KNI) — третья платформа
- iOS — не требуется (архитектура должна допускать добавление позже без переделок)

## Технологический стек

- .NET 8 (хост `Lines.Android` — net9.0-android, см. «Статус Android» ниже)
- MonoGame 3.8.5.1 (шаблоны MonoGame.Templates.CSharp 3.8.5.1;
  в проектах указано `3.8.*`): DesktopGL для Windows, Android-таргет для Android
- Web: KNI 4.2.9001 (форк MonoGame, `nkast.Kni.Templates`, шаблон
  `kni-blazor-gl`; платформа BlazorGL). KNI 4.3.x не используем -
  его MGCB требует .NET 10 runtime.
- Сборка: `dotnet build` / `dotnet publish`
- Android: требуется установленный Android SDK и workload (`dotnet workload install android` при необходимости)

## Структура решения

```
LinesMonoGame/
├── AGENTS.md                  — этот файл (план и правила проекта)
├── LinesMonoGame.sln          — solution
├── src/                       — все исходники
│   ├── LogicLines/                — игровая логика (.NET 8 class library)
│   │   └── Game.cs                — порт ../../LogicLines/Game.cs с исправлениями
│   ├── Lines.Shared/              — весь игровой код MonoGame (shared project .shproj, mgshared;
│   │   │                            импортируется хостами через <Import ...projitems>,
│   │   │                            отдельно не собирается; в .sln добавлен без Build-конфигураций)
│   │   ├── LinesGame.cs           — корневой класс Game
│   │   ├── Screens/               — экраны (ScreenManager + экраны)
│   │   │   ├── ScreenManager.cs
│   │   │   ├── SplashScreen.cs    — заставка
│   │   │   ├── GameScreen.cs      — игровое поле (координатор: ввод, флоу, звуки)
│   │   │   ├── GameOverOverlay.cs — отрисовка конца игры/ввода имени/рекордов
│   │   │   ├── OverlayDialogs.cs  — общий каркас диалогов поверх поля
│   │   │   ├── SettingsScreen.cs  — настройки
│   │   │   └── SavesScreen.cs     — сохранённые игры
│   │   ├── Rendering/             — отрисовка: BoardRenderer (поле), HudRenderer
│   │   │                            (панели), PedestalRenderer (пьедесталы),
│   │   │                            BallRenderer (шарики), UiTheme (палитра)
│   │   ├── Input/                 — мышь + тач (единая абстракция)
│   │   ├── Localization/          — Strings.cs (ru/en, Strings.Current)
│   │   ├── Settings/              — модель настроек + сериализация
│   │   └── Content/               — текстуры, шрифты, звуки (MonoGame Content Builder)
│   ├── Lines.Windows/             — тонкий хост Windows (DesktopGL)
│   │   └── Program.cs
│   ├── Lines.Android/             — тонкий хост Android
│   │   └── Activity1.cs
│   └── Lines.Web/                 — тонкий хост Web (KNI BlazorGL, Blazor WASM)
│       ├── Pages/Index.razor(.cs) — страница с canvas + игровой цикл (Tick)
│       ├── LocalStorageStore.cs   — IKeyValueStore поверх localStorage
│       ├── JsNamePrompter.cs      — ввод имени через JS prompt()
│       └── Content/Content.Web.mgcb — манифест контента /platform:BlazorGL
│           (ассеты копируются из Lines.Shared/Content целью CopySharedContent)
├── tests/                     — тесты
│   └── LogicLines.Tests/          — юнит-тесты: логика, сервисы настроек/рекордов/сейвов, локализация
├── docs/                      — документация
│   ├── code-review.md           — результаты код-ревью и план исправлений (ветка feature/code-review)
│   └── leaderboard-online.md    — план онлайн-доски лидеров (пока только план)
├── .github/workflows/build.yml — CI для ПУБЛИЧНОГО репозитория GitHub (пути в нём
│                                — от корня публичного репо = содержимое LinesMonoGame/):
│                                тесты + сборка установщика/portable/APK + Web на Pages
├── README.md / LICENSE        — описание и лицензия MIT (для публикации на GitHub)
└── build/                     — скрипты сборки/публикации и релизные артефакты
    ├── publish-windows.ps1
    ├── publish-android.ps1
    ├── publish-web.ps1            — релиз Web → out/web/wwwroot (статический сайт)
    ├── publish-web-apache.md      — инструкция публикации на Apache 2.4 (vhost lines.local)
    ├── installer.iss            — сценарий Inno Setup (установщик Windows)
    ├── build-installer.ps1      — self-contained publish + ISCC → out/LinesSetup-*.exe
    └── publish-portable.ps1     — portable: один exe (single-file) → out/portable/Lines.exe
```

Принцип: вся логика и отрисовка — в `LogicLines` и `Lines.Shared`;
проекты-хосты содержат только точку входа и платформенные настройки.

## Фактическое состояние (после Этапа 2)

- Шаблоны: MonoGame.Templates.CSharp 3.8.5.1; пакеты MonoGame 3.8.5.1.
- `src/LogicLines` — net8.0 class library, собирается без ошибок и предупреждений.
- `src/Lines.Shared` — классический shared project (mgshared, `.shproj` + `.projitems`).
  В solution добавлен вручную (тип `{D954291E-...}`), Build-конфигураций нет:
  `dotnet build` solution его пропускает. Хосты импортируют
  `Lines.Shared.projitems` через `<Import>`, контент (`Content/Content.mgcb`)
  подключается отсюда же через `MonoGameContentReference`.
- `src/Lines.Windows` — mgdesktopgl, целевой фреймворк **net8.0**; ссылается
  на `LogicLines` и `Lines.Shared`; собирается и запускается без исключений.
  Заголовок окна — кириллица «Шарики»: DesktopGL передаёт `Window.Title`
  в SDL как ANSI (кириллица превращается в «?????»), поэтому на Windows
  заголовок дополнительно ставится через Win32 `SetWindowTextW` (Unicode)
  на `Process.MainWindowHandle` — НЕ на `Window.Handle` (это указатель
  SDL_Window, а не HWND). Повтор попытки — каждый кадр, пока окно не
  появится (`LinesGame.EnsureTitle`).
- `src/Lines.Android` — mgandroid, **net9.0-android**. Собирается:
  `build/publish-android.ps1` (или `dotnet publish` с
  `-p:AndroidSdkDirectory`, SDK не авто-детектится без ANDROID_HOME).
  Проверено: `com.valkimi.lines-Signed.apk` (~10,7 МБ) + `.aab`
  собираются из чистого состояния. SDK ставился через
  `-t:InstallAndroidDependencies -p:AcceptAndroidSDKLicenses=true
  "-p:AndroidSdkDirectory=%LOCALAPPDATA%\Android\Sdk"`.
- Сборка solution целиком невозможна из-за `Lines.Android`; собирать
  по проектам: `dotnet build src/LogicLines/LogicLines.csproj`,
  `dotnet build src/Lines.Windows/Lines.Windows.csproj`,
  `dotnet build src/Lines.Web/Lines.Web.csproj`,
  `dotnet test tests/LogicLines.Tests/`.
- **Этап 2 выполнен.** Реализовано:
  - `Screens/GameScreen.cs` — игровой экран с полным игровым циклом:
    выбор шарика, поиск пути, перемещение, появление новых шариков,
    сжигание линий, счёт, конец игры.
  - `Rendering/`: программная отрисовка поля (масштабирование под размер
    окна), шариков (круг + градиент + блик), анимации перемещения
    и сжигания, следы шариков.
  - `Input/`: единая абстракция мышь + тач.
  - `Content/MainFont.spritefont` — шрифт для текста (счёт, следующие шарики).
  - `tests/LogicLines.Tests/` — юнит-тесты логики (поиск пути, сжигание
    линий горизонталь/вертикаль/диагональ, очки, конец игры, регрессия
    на `rand.Next`, восстановление сохранённой партии `Game.Restore`,
    фуззер инварианта free_fields), а также тесты сервисов
    Settings/Results/Saves (через in-memory IKeyValueStore) и локализации
    (Strings.Resolve/Plural) — файлы Settings/*.cs и Localization/Strings.cs
    подключены в тестовый .csproj через <Compile Include> напрямую.
- **Этап 3 выполнен.** Реализовано:
  - `Screens/IScreen.cs` — контракт экрана (Update/Draw с Viewport).
  - `Screens/ScreenManager.cs` — переключение экранов; владеет
    `SpriteBatch.Begin/End` (экраны рисуют только между ними).
  - `Screens/SplashScreen.cs` — заставка: надпись «Шарики» сверху,
    программно нарисованное поле с декоративными шариками и красным
    шариком в короне в центре, строка состояния «Версия X.Y.Z Автор:
    Val + Kimi» (версия из `AssemblyInformationalVersion`, автор —
    константа `Author`). Переход в игру по клику/тапу или через
    `SplashDurationSeconds = 10` (константа, скорректировать после
    первых тестов). Картинка программная — `splash.png` на Этапе 5.
  - `LinesGame` стартует с заставки, `GameScreen` создаётся лениво
    при переходе.
- **Этап 4 выполнен.** Реализовано:
  - `Settings/GameSettings.cs` — модель: `SoundEnabled`, `Volume` (0-100),
    `ShowBallTrails`, `Fullscreen`; значения по умолчанию — инициализаторы.
  - `Settings/SettingsService.cs` — загрузка/сохранение JSON
    (`%AppData%/Lines/settings.json` на Windows; на Android — внутренняя
    директория приложения; только `Path.Combine`). `Update(mutation)`
    сохраняет и шлёт событие `Changed`.
  - `Screens/SettingsScreen.cs` — UI программный (мышь + тач): переключатели
    «Звук» и «Следы шариков», ползунок «Громкость» (клик/перетаскивание,
    в `PointerInput` добавлены `IsDown`/`Position`), сегмент «Окно /
    Полный экран», кнопка «Назад». Изменения применяются сразу.
  - Применение: громкость → `SoundEffect.MasterVolume`; fullscreen →
    `GraphicsDeviceManager.IsFullScreen` + `ApplyChanges` (немедленно);
    следы → `GameScreen.ShowTrails` читается из настроек каждый кадр.
  - Навигация: кнопка «Настройки» на панели GameScreen (`SettingsRequested`),
    «Назад» возвращает в игру; экраны переключаются через ScreenManager.
- **Этап 5 выполнен.** Реализовано:
  - Звуковые эффекты: `build/generate-sounds.py` (Python, stdlib `wave`)
    синтезирует 5 WAV в `Content/Sounds/`: `select` (выбор шарика),
    `move` (шаг перемещения), `strip` (сжигание линии), `appear`
    (появление новых шариков), `gameover` (конец игры). 16-бит PCM,
    22050 Гц, моно. Подключены через MGCB (`SoundEffectProcessor`).
  - GameScreen: загружает эффекты, проигрывает по событиям игры;
    при выключенном звуке не проигрываются, громкость — через
    `SoundEffect.MasterVolume` (задаётся в LinesGame.ApplySettings).
  - `build/generate-splash.py` (Python + Pillow) генерирует
    `Content/splash.png` (892×892): поле 9×9, декоративные шарики
    из тех же текстур, что в игре (`Content/Balls/`), в центре -
    персонаж-лидер (`build/assets-src/king_leader.png`) с тенью.
    SplashScreen рисует PNG вместо программной отрисовки.
  - Ассеты восстанавливаются скриптами из build/ (генераторы
    закоммичены, WAV/PNG перегенерируемы, но тоже в git).
- **Этап 6 выполнен.** Реализовано:
  - Таблица рекордов: `Settings/ResultEntry.cs` + `ResultsService.cs`
    (порт логики старого result.json: топ-10 по убыванию счёта,
    счёт 0 и пустое имя не сохраняются, на имя остаётся лучший
    результат; хранение - `%AppData%/Lines/results.json`).
    У записи есть дата/время игры (`Date`, `dd.MM.yy HH:mm`) -
    показывается в таблице правой колонкой в относительном виде
    («30 минут назад», как в окне сохранений) через общий хелпер
    `Screens/RelativeTime.cs` (используется и в SavesScreen). `ResultsService.Clear()`
    очищает таблицу.
  - Флоу конца игры в GameScreen: при Game Over - если счёт в топ -
    ввод имени (поле с мигающим курсором, набор с клавиатуры:
    латиница/цифры, Enter - сохранить, кнопки «Сохранить»/«Пропустить»),
    затем таблица рекордов с подсветкой свежей записи; клик/Enter -
    новая игра.
  - Имя игрока: `GameSettings.PlayerName`. При первом запуске (имя
    пустое) поверх поля показывается «Добро пожаловать! Как вас зовут?»
    (на Windows - экранное поле, на Android - нативный диалог, см.
    `INamePrompter`); «Пропустить» откладывает вопрос до следующего
    запуска. При записи рекорда поле ввода предзаполняется этим именем,
    изменённое имя сохраняется обратно в настройки.
  - В настройках - строка «Рекорды» с кнопкой «Очистить»: двухшаговое
    подтверждение (первый клик - «Подтвердить?» красным, повторный
    в течение 4 секунд - очистка, затем сброс).
  - Сохранение/загрузка партий: `Settings/GameSave.cs` (поле построчно,
    включая резервации -1 под подсказки; следующие шарики x,y,color;
    счёт, дата) + `Settings/SavesService.cs` (10 слотов: слот 0 -
    автосохранение, 1..9 - ручные; `%AppData%/Lines/saves.json`).
    `Game.CanSave`/`FieldMap`/`NextBalls`/`CurrentScore` и
    `Game.Restore(map, balls, score)` - восстановление с перестройкой
    free_fields. Автосохранение - при выходе (RequestExit) и по
    событию Deactivated (сворачивание, уход в фон на Android);
    оконченная/занятая игра не сохраняется. Отключается настройкой
    `AutoSaveEnabled` (строка «Автосохранение» в настройках, по
    умолчанию вкл).
  - Вопрос при выходе после ручного сохранения: GameScreen запоминает
    слот (`MarkSaved`, событие `SavesScreen.GameSaved`) и считает ходы
    после него (инкремент в HandleBoardClick, когда ClickBox нашёл
    путь - признак `game.IsBusy` сразу после клика). При выходе с
    сделанными ходами - оверлей «Обновить ваше сохранение?»:
    «Да» - перезапись слота игрока, «Нет» - выход без автосохранения,
    «Отмена» - остаться (событие `ExitPromptResolved`, разрешает
    LinesGame). Ходов не было - выход молча и без автосохранения
    (`HasUnmovedManualSave`). Загрузка сейва к слоту не привязывается.
  - `Screens/SavesScreen.cs` (кнопки «Сохранить»/«Загрузить» на нижней
    панели GameScreen):
    сетка слотов всегда 2 строки x 5 колонок; в ячейке - превью поля
    9x9 с сеткой, счёт и относительное время («30 минут назад»,
    «2 часа назад», «1 день назад», месяцы/годы; только целые,
    русская плюрализация - хелпер `Screens/RelativeTime.cs`), слот 0 помечен полупрозрачным
    водяным знаком «АВТО» по центру превью (вручную недоступен).
    Вкладки «Загрузить»/«Сохранить»: загрузка - тап по занятой ячейке,
    сохранение - тап по пустой; перезапись занятой - двухшаговое
    подтверждение «Перезаписать?» (4 секунды).
  - ВНИМАНИЕ: в строках UI допустимы только символы из шрифтов
    (ASCII 32-126 + кириллица 1024-1279) - иначе SpriteFont падает
    с исключением (был вылет из-за «·» в подписи ячейки).
  - Иконки: `build/generate-icons.py` (персонаж-лидер
    `build/assets-src/king_leader.png` на тёмном фоне)
    генерирует `Lines.Windows/Icon.ico` + `Icon.bmp` и
    `Lines.Android/Resources/Drawable/Icon.png`.
  - Иконки кнопок: `build/generate-buttons.py` генерирует 6 цветных
    спрайтов 256x256 (плоский двухтональный стиль: светлый силуэт
    слева/тёмный справа + тёмная обводка, суперсэмплинг 4x) в
    `Content/Buttons/`: `btn_new_game`, `btn_save`, `btn_load`,
    `btn_settings`, `btn_records`, `btn_exit`. Подключены в MGCB;
    используются на нижней панели GameScreen (6 квадратных кнопок-
    иконок вместо программной отрисовки; `Rendering/IconRenderer.cs`
    удалён).
  - Android-манифест: label «Шарики», иконка `@drawable/Icon`;
    csproj: `ApplicationId=com.valkimi.lines`, `ApplicationVersion=1`,
    `ApplicationDisplayVersion=1.0.0`.
  - Релизная сборка Windows: `build/publish-windows.ps1`
    (`dotnet publish -c Release` → `build/out/windows/`, проверено:
    exe запускается, контент в комплекте). `build/publish-android.ps1`
    - проверяет android workload и публикует APK (нужен
    `dotnet workload install android` - решение пользователя).
  - Установщик Windows: `build/build-installer.ps1` (self-contained
    publish win-x64 - .NET на машине пользователя не нужен - затем
    Inno Setup 6 по `build/installer.iss`) → `build/out/LinesSetup-1.0.0.exe`
    (~31 МБ). Установка per-user в `%LocalAppData%\Programs\Шарики`,
    без прав администратора; русский мастер, ярлык в «Пуск» и
    опционально на рабочем столе, деинсталлятор. Нативные рантаймы
    MonoGame для linux/osx/arm64 из установщика исключены.
    Inno Setup ставится через `winget install JRSoftware.InnoSetup`
    (per-user в `%LocalAppData%\Programs\Inno Setup 6`). Версия игры -
    `<Version>` в `Lines.Windows.csproj` (и `AppVersion` в
    installer.iss - держать синхронно).
  - Portable-версия Windows: `build/publish-portable.ps1` -
    single-file publish (`PublishSingleFile` + `EnableCompressionInSingleFile`
    + `IncludeAllContentForSelfExtract`) → `build/out/portable/Lines.exe`
    (~38 МБ, один файл, установка не нужна; при запуске самораспаковка
    во временную папку, первый старт чуть медленнее). Настройки/сейвы
    общие с установленной версией (`%AppData%/Lines`).
- `build/capture-window.ps1`, `build/click-client.ps1` — служебные скрипты
  захвата окна и синтетического клика для проверок (не часть игры).
- `build/preview-badge-variants.py`, `build/preview-badge-watermark.py` —
  генераторы макетов ячейки сохранения (варианты бейджа «Авто») в
  `build/out/`; использовались для выбора дизайна, не часть игры.

## Экраны игры

### 1. Экран заставки (SplashScreen)

- Верх экрана: надпись **«Шарики»** (крупный шрифт).
- Большая часть экрана: картинка с изображением игрового поля,
  в центре поля — **красный шарик в короне**.
- Внизу экрана: строка состояния — **версия игры и автор**.
  Автор — константа: **«Val + Kimi»**.
- Поведение: переход в игру по клику/тапу **ИЛИ** автоматически через
  **10 секунд** после запуска (что наступит раньше). Длительность —
  константа `SplashDurationSeconds = 10`, скорректировать после первых тестов.
- Картинка заставки — отдельный ассет `Content/splash.png` (подготовить;
  временно допускается программная отрисовка поля + шарика + короны).

### 2. Игровой экран (GameScreen)

- Поле 9×9, клетки масштабируются под текущий размер окна/экрана.
- Отображение счёта, трёх «следующих» шариков.
- Кнопки/горячие клавиши: новая игра, настройки, выход на заставку.
- Анимации: перемещение шарика по пути, «подпрыгивание» выбранного шарика,
  сжигание собранных линий.
- **Следы шариков**: показ пути (следов) при перемещении шарика —
  включается/выключается в настройках (аналог `Item.path` в старой версии).

### 3. Экран настроек (SettingsScreen)

Параметры (сохраняются между запусками в файл настроек):

| Настройка | Значения | По умолчанию |
|---|---|---|
| Звук | вкл/выкл | вкл |
| Громкость | ползунок 0–100% | 80% |
| Следы шариков (показ пути) | вкл/выкл | вкл |
| Режим экрана | окно / полный экран | окно |
| Автосохранение | вкл/выкл | вкл |
| Язык | авто (по локали системы) / русский / english | авто |

- Локализация (ru/en): все UI-строки - свойства
  `Localization/Strings.cs` (базовый класс - английский/fallback,
  `StringsRu` - русский с трёхформенной плюрализацией; активный язык -
  `Strings.Current`, ставится в `LinesGame.ApplySettings` по настройке
  `GameSettings.Language`). "auto" - по `CultureInfo.CurrentUICulture`
  (Web: локаль браузера); русская локаль → Ru, остальные → En.
  Относительное время - методы `MinutesAgo/HoursAgo/...` в Strings.
  Тексты нативных диалогов (Android `Activity1`, Web `JsNamePrompter`)
  берутся из Strings.Current. «Шарики» - бренд, не переводится.
  Установщик двуязычный (installer.iss: russian+english,
  `ShowLanguageDialog=auto`, строки в `[CustomMessages]`).

- Отдельная строка «Рекорды» с кнопкой «Очистить» (двухшаговое
  подтверждение) — очищает таблицу рекордов через `ResultsService.Clear()`.

- Применение режима экрана — сразу после изменения (borderless fullscreen
  через `GraphicsDeviceManager.IsFullScreen` / `HardwareModeSwitch`).
- Хранение: JSON-файл в платформенной папке пользователя
  (`Environment.SpecialFolder.ApplicationData` на Windows,
  внутренняя директория приложения на Android). НЕ использовать
  `Directory.GetCurrentDirectory()` и обратные слэши в путях —
  только `Path.Combine`.

## Звуки (Content, формат .wav/.ogg через MGCB)

- `select` — выбор шарика
- `move` — перемещение по пути
- `strip` — сжигание линии
- `appear` — появление новых шариков
- `gameover` — конец игры
- Воспроизведение через `SoundEffect`, громкость — `SoundEffect.MasterVolume`
  из настроек; при выключенном звуке эффекты не проигрываются.

## Правила миграции логики (LogicLines)

Портировать `../LogicLines/Game.cs` со следующими обязательными исправлениями:

1. **Баг `rand.Next(0, free_fields.Length - 1)`** — верхняя граница
   исключительная, последнее свободное поле никогда не выбиралось.
   Исправить на `rand.Next(free_fields.Length)`.
2. **Магические числа**: `path = new Ball[81]`, `strip = new Ball[99]`,
   `free_fields = new FieldAddress[81]` — заменить на `List<T>`,
   размер не должен зависеть от хардкода 9×9.
3. `RemoveFreeField`/`AddFreeField` (копирование массивов) — заменить
   на операции `List<FieldAddress>`.
4. Сохранить интерфейс обратных вызовов (`ShowItem`/`ShowScore` или
   события `EventHandler`) — UI-слой не должен проникать в логику.
5. Настройка «следы шариков» выключается на уровне UI: логика по-прежнему
   шлёт `Item.path`, а экран их просто не рисует.

## Планы на будущее

- **Уменьшение Web-публикации** (сейчас ~54 МБ; из них Content 37 МБ,
  `_framework` 17 МБ):
  - `/compress:True` в `Content.Web.mgcb` - LZ4-сжатие XNB, главный
    резерв (шрифты MainFont128 19 МБ + MainFont64 5 МБ в несжатом
    Color; KNI читает сжатые XNB). Проверить загрузку шрифтов на Web.
  - Урезать MainFont128 (нужен только на огромных экранах) или
    сократить набор глифов шрифтов.
  - `dotnet workload install wasm-tools` (нужны права администратора) -
    в Release включаются IL-trimming и Brotli-пресжатие; ожидаемо
    `_framework` 17 МБ -> 8-10 МБ (по сети ~4-5 МБ). После включения -
    полный прогон игры (trimming может резать reflection).
    AOT (`RunAOTCompilation`) НЕ включать: быстрее, но wasm растёт
    в 2-3 раза.
  - Отдача предсжатых .br на Apache (mod_brotli или rewrite-правила;
    сейчас в index.html `enableBrotliDecompression = false`).

## Этапы работ

### Этап 1. Каркас
1. Установить MonoGame templates (`dotnet new install MonoGame.Templates.CSharp`).
2. Создать solution `LinesMonoGame.sln` в корне `LinesMonoGame/`, проекты
   строго по структуре `src/`: `src/LogicLines` (.NET 8), `src/Lines.Shared`,
   `src/Lines.Windows` (mgdesktopgl), `src/Lines.Android` (mgandroid).
3. Портировать и исправить `Game.cs` (см. правила миграции).
4. Проверить: пустое окно запускается на Windows, проект Android собирается
   (требует предварительной установки android workload — см. «Фактическое состояние»).

### Этап 2. Игровой экран (MVP)
1. Отрисовка поля 9×9 с масштабированием под размер окна.
2. Ввод: клик мыши (Windows) и тап (Android) через единую абстракцию.
3. Программная отрисовка шариков (круг + градиент + блик) —
   без зависимости от PNG-ассетов старой версии.
4. Полный игровой цикл: выбор, поиск пути, перемещение, появление
   новых шариков, сжигание линий, счёт, конец игры.
5. Анимации перемещения и сжигания; следы шариков (с учётом настройки).

### Этап 3. Экран заставки
1. Разметка по спецификации (см. раздел «Экран заставки»).
2. Надпись «Шарики», картинка поля с красным шариком в короне,
   строка состояния с версией и автором.
3. Версия берётся из `AssemblyInformationalVersion`, автор — константа
   «Val + Kimi».
4. Переход на игровой экран по клику/тапу или автоматически через
   `SplashDurationSeconds` (10 секунд).

### Этап 4. Экран настроек
1. UI: чекбокс «Звук», ползунок громкости, чекбокс «Следы шариков»,
   переключатель «Окно / Полный экран», кнопка «Назад».
2. Модель настроек + JSON-сериализация + загрузка при старте.
3. Применение: громкость → `SoundEffect.MasterVolume`,
   режим экрана → немедленное переключение fullscreen,
   следы → флаг в GameScreen.

### Этап 5. Звук и контент
1. Подобрать/сгенерировать звуковые эффекты, подключить через MGCB.
2. Подключить звуки к событиям игры с учётом настроек.
3. Подготовить `splash.png` (поле + красный шарик в короне) — заменить
   временную программную отрисовку, если она использовалась.

### Этап 6. Полировка и сборка
1. Таблица рекордов (порт логики result-файла → JSON в папке пользователя).
2. Полноэкранный режим и масштабирование: проверить на разных разрешениях
   и DPI; на Android — разные соотношения сторон, вырезы экрана (safe area).
3. Иконка приложения, название пакета Android, versionCode/versionName.
4. Релизные сборки: `dotnet publish -c Release` (Windows),
   подписанный APK/AAB (Android).

## Правила для агента

- **Проверку доработок в запущенной игре выполняет пользователь.** Не
  запускать игру для самопроверки, не делать скриншоты окна/экрана и
  синтетические клики (capture-window.ps1, click-client.ps1,
  fill-board.ps1) без запроса. Если нужна проверка в игре — собрать
  проект и попросить пользователя проверить.

## Известные ограничения / решения

- **Web (Этап 8, KNI BlazorGL)**:
  - Хост `src/Lines.Web` — Blazor WebAssembly (net8.0), пакеты
    `nkast.Xna.Framework.*` + `nkast.Kni.Platform.Blazor.GL` версии
    **4.2.9001** (4.3.x не подходит: его MGCB требует .NET 10 runtime).
    Шаблон `kni-blazor-gl` (ставится: `dotnet new install nkast.Kni.Templates`).
    Игровой цикл — по шаблону KNI: JS `initRenderJS`/`requestAnimationFrame`
    → `Index.TickDotNet` → `game.Tick()` (`Pages/Index.razor.cs`).
  - Хранение: вся работа с файлами настроек/сейвов/рекордов идёт через
    `Settings/IKeyValueStore.cs` (TryRead/Write JSON-текстов по имени).
    По умолчанию `FileKeyValueStore` (прежние файлы `%AppData%/Lines/*.json`);
    на Web хост передаёт `LocalStorageStore` (localStorage, ключи
    `lines.settings`/`lines.saves`/`lines.results`, синхронный интероп
    `IJSInProcessRuntime`). `LinesGame` принимает хранилище в конструкторе
    (`new LinesGame(store)`).
  - Ввод имени — `JsNamePrompter` (браузерный `prompt()`); `Window.TextInput`
    в BlazorGL нет, подписка в `LinesGame` отключена под
    `#if ANDROID || BLAZORGL` (константа BLAZORGL задаётся в csproj).
    Отмена prompt() = «Пропустить», как на Android.
  - Выход: `Game.Exit()` на BlazorGL бросает PlatformNotSupportedException —
    обёрнут в `LinesGame.TryExit()`; веб-хост подписан на `ExitRequested`
    и возвращает игру на заставку (`LinesGame.ShowSplash`, публичный метод).
  - Строка «Окно / Полный экран» в настройках на Web скрыта, как на Android
    (`OperatingSystem.IsBrowser()` рядом с `IsAndroid()` в SettingsScreen
    и `LinesGame.ApplySettings`); канвас всегда на всю страницу.
  - Заголовок: Win32 `SetWindowTextW` пропускается на Web
    (`OperatingSystem.IsBrowser()` в `EnsureTitle`), заголовок страницы —
    `<title>Шарики</title>` в `wwwroot/index.html`.
  - Контент: общие ассеты НЕ дублируются в git - цель `CopySharedContent`
    в `Lines.Web.csproj` копирует png/wav/spritefont из
    `Lines.Shared/Content` в `Lines.Web/Content` перед `RunContentBuilder`
    (эти копии в .gitignore), а `Content/Content.Web.mgcb` собирает их с
    `/platform:BlazorGL` и `/profile:HiDef`. Отличия от десктопного
    манифеста: платформа, профиль и шрифты в `TextureFormat=Color`
    (на WebGL нет DXT). MGCB не умеет `/build` с путями вне своей папки -
    поэтому копирование, а не прямые ссылки на `../Lines.Shared/Content`.
  - Профиль графики: BlazorGL по умолчанию создаёт устройство Reach
    (макс. текстура 2048) - текстура MainFont128 с кириллицей больше и
    игра падала в `FontSet..ctor` («Reach profile supports a maximum
    Texture2D size of 2048»). Поэтому в `LinesGame` выставлен
    `_graphics.GraphicsProfile = GraphicsProfile.HiDef` (безвредно для
    DesktopGL/Android - там он и так HiDef).
  - JS-биндинги: в `wwwroot/index.html` скрипты `_content/nkast.Wasm.*`
    должны совпадать по версии с пакетами KNI: для KNI 4.2 - файлы
    `*.8.0.11.js` (НЕ `*.10.0.3.js` из шаблона 4.3 - с ними runtime
    падает с нечитаемым `mono_wasm_abort` ещё до managed-исключений).
  - Диагностика: в `index.html` обработчики `error`/`unhandledrejection`
    и перехват исключений в `Index.TickDotNet` выводят текст ошибки в DOM
    (`#jsError`) - без неё в WASM виден только «An unhandled error has
    occurred.».
  - **PWA / офлайн-режим**: `wwwroot/manifest.webmanifest` (иконки
    `icon-192.png`/`icon-512.png` - персонаж-лидер на тёмном фоне),
    service worker. В wwwroot два файла: `service-worker.js` - пустышка
    для разработки, `service-worker.published.js` - боевой: на install
    кэширует ВСЕ ассеты из `service-worker-assets.js` (генерируется SDK
    при публикации, см. `ServiceWorkerAssetsManifest` и item
    `<ServiceWorker Include="wwwroot\service-worker.js"
    PublishedContent="wwwroot\service-worker.published.js" />` в csproj);
    fetch - cache-first, навигация -> закэшированный index.html; имя кэша
    = версия манифеста (старый кэш удаляется в activate). На
    localhost/127.0.0.1 кэш выключен (isDevHost). ВАЖНО: браузеры
    регистрируют service worker только по HTTPS или на localhost - для
    lines.local нужен TLS (см. build/publish-web-apache.md, раздел PWA).
    Проверено: статическая отдача build/out/web + остановка сервера -
    игра загружается и работает полностью из кэша.
  - Релиз: `build/publish-web.ps1` → статический сайт
    `build/out/web/wwwroot` (проверено: 23 .xnb в комплекте). Для
    уменьшения размера (~54 МБ без оптимизаций) можно поставить
    `dotnet workload install wasm-tools` - решение пользователя.
  - Локальный запуск: `dotnet run --project src/Lines.Web` (DevServer),
    либо любой статический сервер над `build/out/web/wwwroot`.

- **Android (Этап 7)**:
  - Immersive-полноэкранный режим — в `Activity1.HideSystemBars`
    (API 30+: `WindowInsetsController`, старее: legacy `SystemUiFlags`),
    повторное скрытие в `OnWindowFocusChanged`. Строка режима экрана
    в настройках на Android скрыта (`SettingsScreen.RowCount = 3`),
    `LinesGame.ApplySettings` переключение fullscreen на Android
    пропускает - там всегда полный экран.
  - Выход: `Game.Exit()` на Android не завершает Activity, поэтому
    `LinesGame.ExitRequested` → `RequestExit()`, а `Activity1` подписан
    и вызывает `FinishAffinity()`. Esc/Back и кнопка «Выход» работают
    одинаково: на экране игры открывается подтверждение «Выйти из
    игры?» (Да/Нет, GameScreen `exitConfirmOpen`; «Да» →
    `GameScreen.ExitRequested` → `RequestExit`); на любом другом
    экране (заставка, настройки, сохранения) Esc возвращает на экран
    игры (`LinesGame.HandleEscape`). Esc обрабатывается по фронту
    нажатия (`_backWasDown`), открытые оверлеи игрового экрана
    закрывает (вопрос о сохранении - как «Отмена»).
  - Портретная ориентация (`BoardLayout.IsPortrait`): внизу строка
    состояния `StatusBarRect` с шестью кнопками-иконками (как и в
    ландшафте); на верхней панели только счёт и подсказки следующих
    шариков (сдвинуты влево). Пьедесталы рисуются только в ландшафте.
  - Ввод имени для рекордов на Android — платформенный диалог:
    интерфейс `Input/INamePrompter` (shared), реализация в `Activity1`
    (AlertDialog + EditText, `SoftInput.StateVisible`); хост ставит
    `LinesGame.NamePrompter`. В MonoGame 3.8.5 `GamerServices.Guide`
    отсутствует - не использовать. Отмена диалога = «Пропустить».
    Диалог - в теме `Theme.DeviceDefault.Dialog.Alert` (системные
    цвета), EditText создаётся в `dialog.Context` - иначе цвет текста
    берётся из темы Activity и совпадает с фоном. Внутриигровое поле
    ввода при наличии `namePrompter` не рисуется. На Windows остаётся
    экранное поле с вводом через `Window.TextInput`.
- Проверка разных разрешений/DPI и safe area на Android — вручную,
  после установки android workload.
- Инвариант логики: `free_fields` == клетки с `map==0`. Фуззер
  (`tests/LogicLines.Tests/FuzzTests.cs`, 60 партий) проверяет его
  после каждого `Step`. Нарушение — источник зависания в
  `SelectNextBall` (исправлено в `699ec65`).
- Размеры поля считаются от `GraphicsDevice.Viewport` при каждом
  `OnResize` — никаких фиксированных 800×571, как в старой версии.
- Между верхней панелью и полем — явный зазор (`BoardLayout`: gap =
  max(8, panelHeight/4)); рамка поля рисуется на 4px выше начала поля,
  без зазора она перекрывает панель.
- Текстуры шариков (`BallRenderer`): готовые PNG 512px из контента
  (`Content/Balls/ball_1..6.png`), генерируются `build/generate-balls.py`
  (numpy + Pillow: сферическое освещение, блик, контровый свет,
  суперсэмплинг 4x → гладкая кромка при любом масштабе; палитра
  синхронизирована с `BallRenderer.Palette`). Тень шарика по-прежнему
  рисуется программно. Рантайм-генерации и `EnsureResolution` больше
  нет - 512px достаточно для любого разрешения.
- Полноэкранный режим: `HardwareModeSwitch = false` (borderless в
  нативном разрешении дисплея). Иначе DesktopGL переключает монитор
  в аппаратный режим 800×600 (PreferredBackBuffer по умолчанию) и
  монитор мылит картинку. Сохранённый фулскрин при старте применяется
  отложенно на первом Update (`_applyFullscreenOnFirstUpdate`) —
  `ApplyChanges` во время LoadContent DesktopGL игнорирует.
- Текст: один шрифт в трёх размерах (`MainFont` 24px, `MainFont64`,
  `MainFont128`), выбор через `Rendering/FontSet.Pick(baseScale, out scale)` —
  берётся наименьший шрифт, который не придётся растягивать (апскейл
  SpriteFont в фулскрине даёт пиксели). baseScale считается от
  `FontSet.Base.LineSpacing`, как раньше от единственного шрифта.
- Нижняя панель игрового экрана — шесть квадратных кнопок-иконок
  (спрайты `Content/Buttons/btn_*`): «Новая игра», «Сохранить»,
  «Загрузить», «Настройки», «Рекорды», «Выход» (правая) — как и Esc,
  открывает подтверждение «Выйти из игры?»; после «Да» —
  `GameScreen.ExitRequested` → `LinesGame.RequestExit`. В фулскрине
  системного крестика нет, кнопка выхода обязательна.
- Кнопка «Рекорды» открывает таблицу рекордов поверх игры
  (`recordsOpen`, оверлей как у конца игры); любой клик закрывает её,
  игра при этом продолжается.
- Пьедесталы по бокам поля (`Rendering/PedestalRenderer.cs`): слева —
  персонаж лидера (имя и очки из таблицы рекордов; пустая таблица →
  «Лидер» 100 очков), справа — «Претендент» (текущий игрок).
  Высоты: претендент lerp(min→max) по clamp(счёт/рекорд, 0..1);
  лидер на максимуме, пока его не обогнали, после превышения рекорда
  уменьшается по clamp(рекорд/счёт, 0..1). Не рисуются при полях < 70px (портретные экраны).
  Персонажи — PNG-текстуры `Content/king_leader.png` (красный шарик
  в золотой короне) и `Content/king_player.png` (синий шарик в тёмной
  короне); обрабатываются `build/process-kings.py` из исходников
  `build/assets-src/` (обрезка по альфа-каналу, уменьшение до 512px).
  Пьедестал — колонна: база из двух плит, ствол с каннелюрами, капитель
  из двух плит; высоты плит ограничены пропорцией к ширине колонны
  (`Clamp(colH/9, 6, colW*0.6)`), иначе на высокой колонне плиты
  растягиваются; персонаж стоит на площадке капители, под ним - тень,
  высота колонны резервирует место под персонажа, чтобы не залезать
  на верхнюю панель.
