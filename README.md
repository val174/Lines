# Шарики (Lines)

Классическая головоломка «Шарики» (Lines 98): поле 9×9, собирай линии
из пяти и более шариков одного цвета. Кроссплатформенная реализация
на C# / MonoGame (KNI).

Авторы: **Val + Kimi**.

## Платформы

- **Windows** — установщик (Inno Setup) и portable-версия (один exe).
- **Android** — APK (портретный и ландшафтный режимы).
- **Web** — Blazor WASM (KNI BlazorGL), работает в браузере, PWA
  (офлайн-режим). Публикуется на GitHub Pages.

## Возможности

- Русский и английский интерфейс (автоопределение по локали системы).
- Таблица рекордов (топ-10) с датой игры.
- Сохранение/загрузка: 9 слотов + автосохранение, превью партий.
- Настройки: звук, громкость, следы шариков, полноэкранный режим.
- Заставка, колонны с коронованным шариком-лидером, анимации.

## Структура репозитория

```
src/
├── LogicLines/      — игровая логика (.NET 8, без зависимостей от UI)
├── Lines.Shared/    — shared-код MonoGame (экраны, рендеринг, сервисы)
├── Lines.Windows/   — хост Windows (DesktopGL)
├── Lines.Android/   — хост Android
└── Lines.Web/       — хост Web (Blazor WASM)
tests/               — юнит-тесты и фуззер логики, тесты сервисов
build/               — скрипты сборки/публикации
docs/                — документация (в т.ч. code-review.md)
```

## Сборка (Windows)

Требования: .NET 8 SDK (+ .NET 9 SDK и workload `android` для APK),
Inno Setup 6 для установщика.

```powershell
# тесты
dotnet test tests/LogicLines.Tests/

# запуск (разработка)
dotnet run --project src/Lines.Windows/

# релизные артефакты -> build/out/
powershell -File build/build-installer.ps1   # установщик
powershell -File build/publish-portable.ps1  # portable exe
powershell -File build/publish-android.ps1   # APK/AAB
powershell -File build/publish-web.ps1       # статический сайт
```

## CI / публикация

GitHub Actions (`.github/workflows/build.yml`) при пуше в `main`:
прогоняет тесты, собирает установщик, portable, APK и Web-версию,
публикует Web на GitHub Pages. Артефакты Windows/Android доступны
в разделе Artifacts соответствующего workflow run.

## Лицензия

MIT — см. [LICENSE](LICENSE).
