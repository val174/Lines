using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Lines.Shared.Input;
using Lines.Shared.Localization;
using Lines.Shared.Rendering;
using Lines.Shared.Settings;
using LogicLines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using LogicGame = LogicLines.Game;

namespace Lines.Shared.Screens
{
    // Игровой экран: поле 9x9, верхняя панель со счётом и «следующими» шариками.
    // Вся логика - в LogicLines.Game, экран только подписан на ShowItem/ShowScore.
    // Отрисовка делегирована: BoardRenderer - поле и анимации, HudRenderer -
    // панели и кнопки, PedestalRenderer - пьедесталы, GameOverOverlay +
    // OverlayDialogs - оверлеи; здесь остаются ввод, флоу и звуки.
    public class GameScreen : IScreen
    {
        private const int MapSize = 9;
        private const int NameMaxLength = 16;

        // интервал между шагами логики; за этот же интервал проигрывается
        // анимация одного шага (перемещение на клетку, фаза сжигания)
        private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(90);

        // максимум шагов логики (Step) за один кадр - защита от
        // «догоняющего» прогона сотен шагов после паузы окна
        private const int MaxStepsPerFrame = 4;

        private readonly LogicGame game;
        private readonly PointerInput pointer = new PointerInput();
        private readonly FontSet fonts;
        private readonly Texture2D pixel; // 1x1 белый - для заливок диалогов
        private readonly BoardRenderer boardRenderer;
        private readonly HudRenderer hud;
        private readonly PedestalRenderer pedestals;
        private readonly GameOverOverlay gameOverOverlay;
        private readonly SettingsService settingsService;
        // есть ли сохранённые партии (для диалога новой игры)
        private readonly Func<bool> hasSaves;
        private readonly SoundEffect sndSelect, sndMove, sndStrip, sndAppear, sndGameOver;
        private bool gameOverPlayed;

        // клик по полю, полученный пока логика была занята
        // (движение/сжигание) - повторяем после завершения хода
        private Point? pendingClick;

        // настройка «следы шариков» берётся из модели настроек
        public bool ShowTrails = true;

        // запрос экрана настроек (кнопка на панели)
        public event Action SettingsRequested;

        // запрос экрана сохранённых игр (кнопки «Сохранить»/«Загрузить»
        // на нижней панели и диалог новой игры); true - режим сохранения
        public event Action<bool> SavesRequested;

        // запрос выхода из игры (кнопка на панели; в фулскрине
        // системного крестика нет)
        public event Action ExitRequested;

        // флоу конца игры: сразу после окончания - либо ввод имени
        // (если счёт попал в топ), либо сразу таблица рекордов
        private enum GoStage { None, EnterName, ShowTable }

        private readonly ResultsService results;
        private readonly INamePrompter namePrompter;
        private GoStage goStage;
        // таблица рекордов поверх игры (кнопка «Рекорды» на панели)
        private bool recordsOpen;
        private string playerName = "Player";
        // первый запуск: просим игрока представиться (имя сохраняется
        // в настройках и потом предзаполняет поле рекорда)
        private bool askName;
        private bool nameDialogOpen; // нативный диалог (Android) уже показан
        private int lastResultIndex = -1; // подсветка свежей записи
        private KeyboardState prevKeys;

        // ручное сохранение в этой сессии: слот и число ходов после него.
        // При выходе, если ходы были, спрашиваем «обновить сохранение?»;
        // ходов не было - выходим молча и без автосохранения
        private int savedSlot = -1;
        private int movesSinceSave;
        private bool exitPromptOpen;

        // подтверждение выхода из игры (Esc / кнопка «Выход»)
        private bool exitConfirmOpen;
        // диалог «Начать новую игру?» (кнопка на нижней панели):
        // новая игра / загрузить из сохранённых / отмена
        private bool newGamePromptOpen;

        // слот, в который игрок сохранялся вручную (-1 - не сохранялся)
        public int SavedSlot => savedSlot;

        // true, если было ручное сохранение, после которого не сделано
        // ни одного хода - автосохранение при выходе не нужно
        public bool HasUnmovedManualSave => savedSlot >= 0 && movesSinceSave == 0;

        // пора спросить «обновить сохранение?» перед выходом
        public bool NeedsSavePrompt =>
            savedSlot >= 0 && movesSinceSave > 0 && !exitPromptOpen
            && !exitConfirmOpen && game.CanSave;

        // разрешение вопроса при выходе: true - обновить сохранение,
        // false - выйти без сохранения, null - отмена выхода
        public event Action<bool?> ExitPromptResolved;

        // вызывается из SavesScreen после ручного сохранения
        public void MarkSaved(int slot)
        {
            savedSlot = slot;
            movesSinceSave = 0;
        }

        public void OpenExitPrompt()
        {
            recordsOpen = false;
            exitPromptOpen = true;
        }

        // подтверждение выхода: одинаково для Esc и кнопки «Выход»
        public void OpenExitConfirm()
        {
            recordsOpen = false;
            exitConfirmOpen = true;
        }

        // Esc на игровом экране: открытые оверлеи закрывает, иначе -
        // подтверждение выхода. На других экранах Esc обрабатывает
        // LinesGame (возврат сюда)
        public void HandleEscape()
        {
            if (exitPromptOpen)
            {
                exitPromptOpen = false;
                ExitPromptResolved?.Invoke(null); // отмена выхода
                return;
            }
            if (exitConfirmOpen)
            {
                exitConfirmOpen = false;
                return;
            }
            if (newGamePromptOpen)
            {
                newGamePromptOpen = false;
                return;
            }
            if (recordsOpen)
            {
                recordsOpen = false;
                return;
            }
            if (askName)
                return; // вопрос имени Esc не закрывает
            OpenExitConfirm();
        }

        // кнопка «Новая игра»: если есть сохранённые партии - диалог
        // «новая / загрузить / отмена», иначе (выбирать не из чего) -
        // сразу новая игра
        private void OnNewGameButton()
        {
            if (hasSaves?.Invoke() == true)
            {
                recordsOpen = false;
                newGamePromptOpen = true;
            }
            else
            {
                StartNewGame();
            }
        }

        private void StartNewGame()
        {
            savedSlot = -1;
            movesSinceSave = 0;
            goStage = GoStage.None;
            recordsOpen = false;
            lastResultIndex = -1;
            gameOverPlayed = false;
            exitPromptOpen = false;
            exitConfirmOpen = false;
            newGamePromptOpen = false;
            game.NewGame(); // поле переинициализируется на следующем шаге логики
        }

        private int score;
        private readonly int[] nextColors = new int[3]; // предпросмотр следующих шариков

        // кэш строки счёта: пересобирается при смене счёта (OnScore)
        // или языка, а не каждый кадр
        private string cachedScoreLabel;
        private int cachedScoreValue = -1;
        private string scoreText = "";

        // кэш отформатированных строк таблицы рекордов: пересобирается
        // при открытии таблицы и при изменении данных (ResultsService.Changed),
        // а не каждый кадр (RelativeTime.Format парсит дату)
        private readonly List<GameOverOverlay.ResultRow> resultsRows =
            new List<GameOverOverlay.ResultRow>();
        private bool resultsCacheDirty = true;
        private bool wasTableVisible;

        // действия из обратных вызовов платформенных диалогов: на Android
        // callback INamePrompter приходит с UI-потока, поэтому он только
        // ставит действие в очередь, а исполняем мы его здесь, на игровом
        // потоке, в начале Update (гонка с settings.Update и полем)
        private readonly ConcurrentQueue<Action> pendingActions = new ConcurrentQueue<Action>();

        // события логики накапливаем за шаг и разбираем пакетом
        private readonly List<ShowEvent> events = new List<ShowEvent>();

        private TimeSpan stepTimer;
        private float time; // время с запуска экрана, сек - для дельт анимаций
        // фазы периодических эффектов, обёрнутые по их периоду: иначе
        // в многочасовой сессии float теряет точность и синусоиды
        // «подпрыгивания»/мигания курсора дёргаются
        private static readonly float BouncePeriod = MathF.PI / 3f; // период sin(t*6)
        private float bouncePhase; // [0, BouncePeriod) - подпрыгивание выбранного шарика
        private float blinkPhase;  // [0, 1) - мигание курсора в поле имени
        private bool restoring; // загрузка сохранения: без звуков и анимаций

        public GameScreen(GraphicsDevice device, ContentManager content, SettingsService settingsService,
            ResultsService results, ITextSource textSource, INamePrompter namePrompter = null,
            Func<bool> hasSaves = null)
        {
            this.settingsService = settingsService;
            this.results = results;
            this.namePrompter = namePrompter;
            this.hasSaves = hasSaves;
            textSource.CharTyped += OnCharTyped;
            sndSelect = content.Load<SoundEffect>("Sounds/select");
            sndMove = content.Load<SoundEffect>("Sounds/move");
            sndStrip = content.Load<SoundEffect>("Sounds/strip");
            sndAppear = content.Load<SoundEffect>("Sounds/appear");
            sndGameOver = content.Load<SoundEffect>("Sounds/gameover");
            fonts = new FontSet(content);
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });

            // рендереры делят между собой общие pixel и BallRenderer
            var balls = new BallRenderer(device, content);
            boardRenderer = new BoardRenderer(balls, pixel, MapSize, OnBoardReset);
            hud = new HudRenderer(content, pixel, fonts, balls);
            pedestals = new PedestalRenderer(content, fonts, balls, pixel, results);
            gameOverOverlay = new GameOverOverlay(fonts, pixel);

            // имя игрока из настроек; пусто - первый запуск, спросим
            playerName = string.IsNullOrWhiteSpace(settingsService.Current.PlayerName)
                ? "Player"
                : settingsService.Current.PlayerName;
            askName = string.IsNullOrWhiteSpace(settingsService.Current.PlayerName);

            game = new LogicGame(MapSize, OnShow, OnScore);
            stepTimer = StepInterval; // первый шаг (инициализация поля) - сразу

            // смена данных рекордов инвалидирует кэш строк таблицы
            results.Changed += () => resultsCacheDirty = true;
        }

        private void OnShow(Ball ball, Item item)
        {
            events.Add(new ShowEvent { Ball = ball, Item = item });
        }

        private void OnScore(int value)
        {
            score = value;
            cachedScoreValue = -1; // инвалидация кэша строки счёта
        }

        // эффекты не проигрываются при выключенном звуке в настройках;
        // громкость задаётся глобально через SoundEffect.MasterVolume (LinesGame)
        private void Play(SoundEffect effect)
        {
            if (settingsService.Current.SoundEnabled)
                effect?.Play();
        }

        // ScreenManager вызывает при переключении на этот экран:
        // зажатая кнопка/палец с прошлого экрана не даст фантомный клик
        public void ResetInput() => pointer.Reset();

        public void Update(GameTime gameTime, Viewport viewport)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            time += elapsedSeconds;
            bouncePhase += elapsedSeconds;
            if (bouncePhase >= BouncePeriod)
                bouncePhase -= BouncePeriod;
            blinkPhase += elapsedSeconds;
            if (blinkPhase >= 1f)
                blinkPhase -= 1f;
            ShowTrails = settingsService.Current.ShowBallTrails;

            // действия, поставленные обратными вызовами платформенных
            // диалогов (на Android - с UI-потока), исполняем на игровом
            while (pendingActions.TryDequeue(out Action pending))
                pending();

            // открытие таблицы рекордов - повод пересобрать кэш строк
            // (относительное время в датах могло устареть)
            bool tableVisible = recordsOpen || goStage == GoStage.ShowTable;
            if (tableVisible && !wasTableVisible)
                resultsCacheDirty = true;
            wasTableVisible = tableVisible;

            // первый запуск: просим представиться; на Android - нативный
            // диалог (один раз), на Windows - экранное поле (см. GameOverOverlay)
            if (askName && namePrompter != null && !nameDialogOpen)
            {
                nameDialogOpen = true;
                namePrompter.PromptName(Strings.Current.Welcome, Strings.Current.AskName, playerName, name =>
                    pendingActions.Enqueue(() =>
                    {
                        nameDialogOpen = false;
                        askName = false;
                        if (!string.IsNullOrWhiteSpace(name))
                            SavePlayerName(name);
                    }));
            }

            BoardLayout layout = BoardLayout.Compute(viewport, MapSize);

            pointer.Update();
            if (pointer.Click.HasValue)
                HandleClick(pointer.Click.Value, layout, viewport);

            HandleGameOverKeys();
            UpdateGameOverStage();

            // не более MaxStepsPerFrame шагов логики за кадр: после паузы
            // окна (сворачивание, перетаскивание) накопленный остаток
            // таймера сбрасываем, иначе прогоняются сотни шагов сразу
            stepTimer += gameTime.ElapsedGameTime;
            int steps = 0;
            while (stepTimer >= StepInterval)
            {
                stepTimer -= StepInterval;
                events.Clear();
                game.Step();
                ProcessEvents();
                if (++steps >= MaxStepsPerFrame && stepTimer >= StepInterval)
                {
                    stepTimer = TimeSpan.Zero;
                    break;
                }
            }

            // отложенный клик: повторяем, когда логика освободилась;
            // исполняем с теми же ограничениями, что и обычный клик, -
            // поверх открытых оверлеев (имя/выход/рекорды) он не проходит
            if (pendingClick.HasValue && !game.IsBusy && !game.IsGameOver)
            {
                Point p = pendingClick.Value;
                pendingClick = null;
                if (!askName && !exitConfirmOpen && !exitPromptOpen
                    && !newGamePromptOpen && !recordsOpen)
                    HandleBoardClick(p.X, p.Y);
            }

            // звук конца игры - по переходу в IsGameOver (один раз)
            if (game.IsGameOver && !gameOverPlayed)
            {
                gameOverPlayed = true;
                Play(sndGameOver);
            }
            else if (!game.IsGameOver)
            {
                gameOverPlayed = false;
            }
        }

        private void HandleClick(Point click, BoardLayout layout, Viewport viewport)
        {
            // идёт вопрос «как вас зовут?» - поле недоступно
            if (askName)
            {
                if (namePrompter != null)
                    return; // на Android имя спрашивает нативный диалог
                var l = GameOverOverlay.ComputeLayout(viewport);
                if (l.SaveButton.Contains(click))
                {
                    SavePlayerName(playerName);
                    askName = false;
                }
                else if (l.SkipButton.Contains(click))
                {
                    askName = false; // имя не сохраняем - спросим при следующем запуске
                }
                return;
            }

            // подтверждение выхода из игры - поле недоступно
            if (exitConfirmOpen)
            {
                OverlayDialogs.PromptLayout p = OverlayDialogs.ComputeTwoButtonLayout(viewport);
                if (p.Yes.Contains(click))
                {
                    exitConfirmOpen = false;
                    ExitRequested?.Invoke();
                }
                else if (p.No.Contains(click))
                {
                    exitConfirmOpen = false;
                }
                return;
            }

            // вопрос «обновить сохранение?» перед выходом - поле недоступно
            if (exitPromptOpen)
            {
                OverlayDialogs.PromptLayout p = OverlayDialogs.ComputeThreeButtonLayout(viewport);
                if (p.Yes.Contains(click))
                {
                    exitPromptOpen = false;
                    ExitPromptResolved?.Invoke(true);
                }
                else if (p.No.Contains(click))
                {
                    exitPromptOpen = false;
                    ExitPromptResolved?.Invoke(false);
                }
                else if (p.Cancel.Contains(click))
                {
                    exitPromptOpen = false;
                    ExitPromptResolved?.Invoke(null);
                }
                return;
            }

            // диалог «Начать новую игру?» - поле недоступно
            if (newGamePromptOpen)
            {
                OverlayDialogs.PromptLayout p = OverlayDialogs.ComputeThreeButtonLayout(viewport);
                if (p.Yes.Contains(click))
                {
                    newGamePromptOpen = false;
                    StartNewGame();
                }
                else if (p.No.Contains(click))
                {
                    newGamePromptOpen = false;
                    SavesRequested?.Invoke(false); // выбрать из сохранённых
                }
                else if (p.Cancel.Contains(click))
                {
                    newGamePromptOpen = false;
                }
                return;
            }

            // открытая таблица рекордов - любой клик закрывает её
            if (recordsOpen)
            {
                recordsOpen = false;
                return;
            }

            // нижняя панель: Новая игра / Сохранить / Загрузить /
            // Настройки / Рекорды / Выход
            foreach (PanelButton btn in HudRenderer.Buttons)
            {
                if (!HudRenderer.ButtonRect(layout, btn).Contains(click))
                    continue;
                switch (btn)
                {
                    case PanelButton.NewGame: OnNewGameButton(); break;
                    case PanelButton.Save: SavesRequested?.Invoke(true); break;
                    case PanelButton.Load: SavesRequested?.Invoke(false); break;
                    case PanelButton.Settings: SettingsRequested?.Invoke(); break;
                    case PanelButton.Records: recordsOpen = true; break;
                    case PanelButton.Exit: OpenExitConfirm(); break;
                }
                return;
            }

            if (game.IsGameOver)
            {
                HandleGameOverClick(click, viewport);
                return;
            }

            if (!layout.TryGetCell(click, out int cx, out int cy))
                return;

            // логика занята (движение/сжигание) - ClickBox проигнорирует
            // клик; запоминаем его и повторим, когда ход завершится
            if (game.IsBusy)
            {
                pendingClick = new Point(cx, cy);
                return;
            }

            HandleBoardClick(cx, cy);
        }

        private void HandleBoardClick(int cx, int cy)
        {
            events.Clear();
            game.ClickBox(cx, cy);
            ProcessEvents();

            // выбор шарика - чисто визуальная вещь (подпрыгивание), логикой не отдаётся
            if (boardRenderer.TrySelect(cx, cy))
                Play(sndSelect);

            // путь найден, ход начался (логика занята показом пути/
            // движением) - считаем ходы после ручного сохранения
            if (game.IsBusy)
                movesSinceSave++;
        }

        // --- конец игры: ввод имени и таблица рекордов ---

        private void UpdateGameOverStage()
        {
            if (game.IsGameOver && goStage == GoStage.None)
            {
                goStage = results.Qualifies(score) ? GoStage.EnterName : GoStage.ShowTable;
                lastResultIndex = -1;
                recordsOpen = false; // диалог конца игры главнее
                if (goStage == GoStage.EnterName)
                {
                    // предзаполняем поле именем игрока (можно изменить)
                    if (!string.IsNullOrWhiteSpace(settingsService.Current.PlayerName))
                        playerName = settingsService.Current.PlayerName;
                    if (namePrompter != null)
                        AskNameNative();
                }
            }
            else if (!game.IsGameOver)
            {
                goStage = GoStage.None; // новая игра
            }
        }

        // на Android нет SDL TextInput - имя запрашиваем платформенным
        // диалогом с экранной клавиатурой; отмена - как кнопка «Пропустить».
        // callback приходит с UI-потока - исполнение откладываем через
        // очередь на игровой поток (см. pendingActions в Update)
        private void AskNameNative()
        {
            namePrompter.PromptName(Strings.Current.NewRecordTitle, Strings.Current.EnterNamePrompt, playerName,
                name => pendingActions.Enqueue(() =>
                {
                    name = FilterName(name).Trim();
                    if (name.Length == 0)
                    {
                        goStage = GoStage.ShowTable;
                        return;
                    }
                    playerName = name.Length > NameMaxLength
                        ? name.Substring(0, NameMaxLength)
                        : name;
                    SaveName();
                }));
        }

        // алфавит игровых шрифтов: ASCII 32-126 + кириллица;
        // символы вне него (эмодзи, иероглифы) крашат SpriteFont
        // при DrawString, поэтому отсекаем их на вводе
        private static bool IsNameCharAllowed(char c) =>
            (c >= 32 && c <= 126) || (c >= 'Ѐ' && c <= 'ӿ');

        private static string FilterName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
                if (IsNameCharAllowed(c))
                    sb.Append(c);
            return sb.ToString();
        }

        private void HandleGameOverKeys()
        {
            KeyboardState kb = Keyboard.GetState();
            // ввод имени: при конце игры (рекорд) или при первом запуске
            // («как вас зовут?», только Windows - на Android нативный диалог)
            bool nameEntry = goStage == GoStage.EnterName ||
                (askName && namePrompter == null);
            if (nameEntry)
            {
                // Backspace - физическая клавиша (раскладка не важна);
                // печатные символы приходят через TextInput (событие
                // подписано в конструкторе) - с учётом раскладки,
                // включая кириллицу
                if (Pressed(kb, Keys.Enter))
                {
                    if (goStage == GoStage.EnterName)
                        SaveName();
                    else
                    {
                        SavePlayerName(playerName);
                        askName = false;
                    }
                }
                else if (Pressed(kb, Keys.Back) && playerName.Length > 0)
                    playerName = playerName.Substring(0, playerName.Length - 1);
            }
            else if (goStage == GoStage.ShowTable && Pressed(kb, Keys.Enter))
            {
                game.NewGame();
            }
            prevKeys = kb;
        }

        private void OnCharTyped(char c)
        {
            bool nameEntry = goStage == GoStage.EnterName ||
                (askName && namePrompter == null);
            if (!nameEntry || playerName.Length >= NameMaxLength)
                return;
            if (!IsNameCharAllowed(c))
                return;
            playerName += c;
        }

        private bool Pressed(KeyboardState kb, Keys key) =>
            kb.IsKeyDown(key) && !prevKeys.IsKeyDown(key);

        // имя игрока: в настройки (предзаполнение рекорда при следующих
        // играх); пустое имя игнорируется
        private void SavePlayerName(string name)
        {
            name = name.Trim();
            if (name.Length == 0)
                return;
            playerName = name.Length > NameMaxLength ? name.Substring(0, NameMaxLength) : name;
            settingsService.Update(s => s.PlayerName = playerName);
        }

        private void SaveName()
        {
            // пустое имя - подставляем дефолт, как в ResultEntry:
            // запись рекорда всё равно сохраняется
            if (playerName.Trim().Length == 0)
                playerName = "Player";
            lastResultIndex = results.Add(playerName, score);
            SavePlayerName(playerName); // запоминаем имя для следующих игр
            goStage = GoStage.ShowTable;
        }

        private void HandleGameOverClick(Point click, Viewport viewport)
        {
            if (goStage == GoStage.EnterName)
            {
                if (namePrompter != null)
                    return; // на Android ввод - в нативном диалоге
                var l = GameOverOverlay.ComputeLayout(viewport);
                if (l.SaveButton.Contains(click))
                    SaveName();
                else if (l.SkipButton.Contains(click))
                    goStage = GoStage.ShowTable;
            }
            else if (goStage == GoStage.ShowTable)
            {
                StartNewGame();
            }
        }

        // разбор пакета событий логики: визуальное состояние обновляет
        // BoardRenderer, здесь остаются только звуковые эффекты шага
        private void ProcessEvents()
        {
            boardRenderer.ApplyEvents(events, time,
                out bool playStrip, out bool playMove, out bool playAppear, nextColors);

            // звуки: сжигание главнее шага перемещения;
            // при загрузке сохранения звуки не играем
            if (!restoring)
            {
                if (playStrip)
                    Play(sndStrip);
                else if (playMove)
                    Play(sndMove);
                if (playAppear)
                    Play(sndAppear);
            }
        }

        // полный сброс визуала поля (пакет «empty» от логики):
        // BoardRenderer чистит своё состояние сам, здесь -
        // отложенный клик и предпросмотр «следующих» шариков
        private void OnBoardReset()
        {
            pendingClick = null;
            nextColors[0] = nextColors[1] = nextColors[2] = 0;
        }

        private void ResetVisuals()
        {
            boardRenderer.ResetVisuals();
            OnBoardReset();
        }

        // --- сохранение / загрузка партии ---

        // дешёвая проверка «можно сохранить» (без копирования поля) -
        // для отрисовки SavesScreen; сам снимок делает CaptureSave
        public bool CanCaptureSave => game.CanSave;

        // снимок текущей партии; null - сохранять нельзя (логика занята
        // или игра окончена). Вызывается из SavesScreen и при
        // автосохранении (LinesGame)
        public GameSave CaptureSave()
        {
            if (!game.CanSave)
                return null;

            var save = new GameSave { MapSize = MapSize, Score = score };
            int[,] m = game.FieldMap;
            for (int x = 0; x < MapSize; x++)
                for (int y = 0; y < MapSize; y++)
                    save.Balls[y * MapSize + x] = m[x, y];

            Ball[] nb = game.NextBalls;
            for (int i = 0; i < 3; i++)
            {
                save.NextBalls[i * 3] = nb[i].x;
                save.NextBalls[i * 3 + 1] = nb[i].y;
                save.NextBalls[i * 3 + 2] = nb[i].color;
            }
            return save;
        }

        // загрузка сохранённой партии: логика пересоздаёт состояние
        // и шлёт события Show - зеркала (cellColor/подсказки/счёт)
        // обновляются через ProcessEvents синхронно
        public void RestoreSave(GameSave save)
        {
            if (save == null || save.MapSize != MapSize ||
                save.Balls == null || save.Balls.Length != MapSize * MapSize ||
                save.NextBalls == null || save.NextBalls.Length < 9)
                return;

            var m = new int[MapSize, MapSize];
            for (int x = 0; x < MapSize; x++)
                for (int y = 0; y < MapSize; y++)
                    m[x, y] = save.Balls[y * MapSize + x];

            var nb = new Ball[3];
            for (int i = 0; i < 3; i++)
            {
                nb[i].x = save.NextBalls[i * 3];
                nb[i].y = save.NextBalls[i * 3 + 1];
                nb[i].color = save.NextBalls[i * 3 + 2];
            }

            ResetVisuals();
            goStage = GoStage.None;
            recordsOpen = false;
            lastResultIndex = -1;
            gameOverPlayed = false;
            savedSlot = -1; // загруженная партия не привязана к слоту
            movesSinceSave = 0;

            events.Clear();
            restoring = true;
            game.Restore(m, nb, save.Score);
            ProcessEvents();
            restoring = false;
            boardRenderer.ClearAppear(); // восстановленное поле рисуем сразу, без «появления»
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            BoardLayout layout = BoardLayout.Compute(viewport, MapSize);

            // строка счёта кэширована: пересборка только при смене счёта
            // (OnScore) или языка интерфейса
            string label = Strings.Current.ScoreLabel;
            if (label != cachedScoreLabel || score != cachedScoreValue)
            {
                cachedScoreLabel = label;
                cachedScoreValue = score;
                scoreText = label + score;
            }

            hud.DrawPanel(spriteBatch, layout, scoreText, nextColors);
            boardRenderer.Draw(spriteBatch, layout, time, ShowTrails, bouncePhase);
            hud.DrawStatusBar(spriteBatch, layout);
            pedestals.Draw(spriteBatch, layout, viewport, score);

            if (game.IsGameOver)
            {
                EnsureResultsCache();
                gameOverOverlay.DrawGameOver(spriteBatch, viewport, goStage == GoStage.EnterName,
                    scoreText, playerName, blinkPhase, namePrompter != null, resultsRows, lastResultIndex);
            }
            else if (recordsOpen)
            {
                EnsureResultsCache();
                gameOverOverlay.DrawRecords(spriteBatch, viewport, resultsRows, lastResultIndex);
            }
            else if (askName)
            {
                gameOverOverlay.DrawAskName(spriteBatch, viewport, playerName, blinkPhase, namePrompter != null);
            }

            Strings t = Strings.Current;
            if (exitPromptOpen)
            {
                // вопрос «обновить сохранение?» перед выходом
                OverlayDialogs.DrawDialog(spriteBatch, pixel, fonts, viewport, t.UpdateSavePrompt,
                    OverlayDialogs.ComputeThreeButtonLayout(viewport),
                    t.Yes, UiTheme.Confirm, t.No, UiTheme.Danger, t.Cancel, UiTheme.Neutral);
            }
            if (exitConfirmOpen)
            {
                // подтверждение выхода из игры (Esc / кнопка «Выход»)
                OverlayDialogs.DrawDialog(spriteBatch, pixel, fonts, viewport, t.ExitConfirmPrompt,
                    OverlayDialogs.ComputeTwoButtonLayout(viewport),
                    t.Yes, UiTheme.Danger, t.No, UiTheme.Confirm);
            }
            if (newGamePromptOpen)
            {
                // диалог кнопки «Новая игра»: начать новую / загрузить / отмена
                OverlayDialogs.DrawDialog(spriteBatch, pixel, fonts, viewport, t.NewGamePrompt,
                    OverlayDialogs.ComputeThreeButtonLayout(viewport),
                    t.NewGame, UiTheme.Confirm, t.Load, UiTheme.Neutral, t.Cancel, UiTheme.Danger);
            }
        }

        // пересборка кэша строк таблицы рекордов (счёт/имя/дата уже
        // отформатированы); вызывается из Draw, но реальная работа -
        // только после инвалидации (открытие таблицы, смена данных)
        private void EnsureResultsCache()
        {
            if (!resultsCacheDirty)
                return;
            resultsCacheDirty = false;
            resultsRows.Clear();
            for (int i = 0; i < results.Entries.Count; i++)
            {
                ResultEntry e = results.Entries[i];
                resultsRows.Add(new GameOverOverlay.ResultRow
                {
                    Pos = $"{i + 1}.",
                    Name = e.Name,
                    Score = e.Score.ToString(),
                    // дата - относительное время; у старых записей даты
                    // может не быть - тогда пусто
                    Date = RelativeTime.Format(e.Date),
                });
            }
        }
    }
}
