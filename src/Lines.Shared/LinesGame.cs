using System;
using System.Linq;
using System.Runtime.InteropServices;
using Lines.Shared.Input;
using Lines.Shared.Localization;
using Lines.Shared.Screens;
using Lines.Shared.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Lines.Shared
{
    // ITextSource: SDL text input (Window.TextInput) пересылаем
    // подписчикам (GameScreen) - символы с учётом раскладки
    public class LinesGame : Game, ITextSource
    {
        public event Action<char> CharTyped;
        // Заголовок окна на русском. DesktopGL отдаёт Window.Title в SDL
        // как ANSI и кириллица превращается в «?????», поэтому на Windows
        // дополнительно ставим заголовок напрямую через Win32 (Unicode).
        private const string TitleText = "Шарики";

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private readonly ScreenManager screens = new ScreenManager();
        // хранилище настроек/сейвов/рекордов; null - файловое по
        // умолчанию, на Web хост передаёт localStorage-реализацию
        private readonly IKeyValueStore store;
        private SettingsService settings;
        private ResultsService results;
        private SavesService savesService;
        private GameScreen gameScreen;
        private SettingsScreen settingsScreen;
        private SavesScreen savesScreen;
        private bool _titleSet;
        // сохранённый полноэкранный режим: применяем на первом Update,
        // т.к. ApplyChanges во время LoadContent DesktopGL игнорирует
        private bool _applyFullscreenOnFirstUpdate;

        // запрос на выход из игры; на Android Game.Exit() не завершает
        // Activity, поэтому хост (Activity1) подписывается и вызывает
        // FinishAffinity() сам
        public event Action ExitRequested;

        // платформенный диалог ввода имени; устанавливает хост (Activity1),
        // на Windows остаётся null - там ввод через Window.TextInput
        public INamePrompter NamePrompter { get; set; }

        public void RequestExit()
        {
            // игрок сохранялся вручную и после этого ходил - сначала
            // спрашиваем, обновить ли его сохранение (ответ - в
            // ExitPromptResolved), сам выход откладывается
            if (gameScreen != null && gameScreen.NeedsSavePrompt)
            {
                gameScreen.OpenExitPrompt();
                screens.Set(gameScreen);
                return;
            }
            AutoSaveGame(); // автосохранение партии перед выходом
            ExitRequested?.Invoke();
            TryExit();
        }

        // Game.Exit() в браузере (BlazorGL) бросает
        // PlatformNotSupportedException - вкладку закрыть нельзя
        private void TryExit()
        {
            try { Exit(); }
            catch (PlatformNotSupportedException) { /* Web: выход обрабатывает хост */ }
        }

        // Esc/Back: на экране игры - подтверждение выхода (или закрытие
        // открытого оверлея), на любом другом экране - возврат в игру
        private bool _backWasDown;

        private void HandleEscape()
        {
            if (gameScreen != null && screens.Current == gameScreen)
                gameScreen.HandleEscape();
            else
                ShowGame();
        }

        public LinesGame(IKeyValueStore store = null)
        {
            this.store = store;
            _graphics = new GraphicsDeviceManager(this);            // borderless-полноэкранный режим в нативном разрешении дисплея;
            // иначе HardwareModeSwitch переключает монитор в 800x600
            // (PreferredBackBuffer по умолчанию) и монитор мылит картинку
            _graphics.HardwareModeSwitch = false;
            // BlazorGL по умолчанию создаёт устройство с профилем Reach
            // (макс. текстура 2048) - текстура MainFont128 с кириллицей
            // больше; на DesktopGL/Android профиль и так HiDef
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true; // поле масштабируется под размер окна
            Window.Title = TitleText;
#if ANDROID || BLAZORGL
            // на Android у GameWindow нет TextInput - ввод имени позже
            // (INamePrompter от хоста); на Web (BlazorGL) - аналогично,
            // имя запрашивает JS-диалог через INamePrompter
#else
            Window.TextInput += OnWindowTextInput;
#endif
        }

        // печатные символы от SDL text input - подписчикам (GameScreen)
        private void OnWindowTextInput(object sender, TextInputEventArgs e)
        {
            CharTyped?.Invoke(e.Character);
        }

        protected override void Initialize()
        {
            EnsureTitle();

            base.Initialize();
        }

        // DesktopGL создаёт окно не сразу - повторяем попытку, пока окно не появится.
        // ВАЖНО: Window.Handle у DesktopGL - указатель SDL_Window, НЕ Win32 HWND,
        // поэтому берём главное окно процесса (как в рабочем решении Этапа 2).
        private void EnsureTitle()
        {
            // на Web (WASM) нет Win32-окна, заголовок страницы ставится
            // в index.html; RuntimeInformation на браузере всегда не Windows
            if (_titleSet || OperatingSystem.IsBrowser()
                || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (handle == IntPtr.Zero)
                return;

            if (SetWindowText(handle, TitleText))
                _titleSet = true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // настройки и рекорды: загрузка из JSON + применение при каждом изменении
            settings = new SettingsService(store);
            settings.Changed += ApplySettings;
            results = new ResultsService(store);
            savesService = new SavesService(store);

            // автосохранение партии при сворачивании/потере фокуса
            // (на Android - при уходе в фон); выход - в RequestExit
            Deactivated += (s, e) => AutoSaveGame();

            // стартовый экран - заставка; игровой и настройки создаём лениво
            ShowSplash();

            // в т.ч. сохранённый полноэкранный режим - отложенно, на первый Update
            _applyFullscreenOnFirstUpdate = settings.Current.Fullscreen;
            ApplySettings();
        }

        // возврат на заставку; на Web хост подписывается на ExitRequested
        // и возвращает игру к заставке (вкладку браузера закрыть нельзя)
        public void ShowSplash()
        {
            var splash = new SplashScreen(GraphicsDevice, Content);
            splash.Finished += ShowGame;
            screens.Set(splash);
        }

        private void ShowGame()
        {
            if (gameScreen == null)
            {
                gameScreen = new GameScreen(GraphicsDevice, Content, settings, results,
                    this, NamePrompter,
                    () => savesService.Slots.Any(s => s != null));
                gameScreen.SettingsRequested += ShowSettings;
                gameScreen.SavesRequested += ShowSaves;
                gameScreen.ExitRequested += RequestExit;
                // ответ на вопрос «обновить сохранение?»: «Да» -
                // перезаписываем слот игрока, «Нет» - без автосохранения,
                // «Отмена» - остаёмся в игре
                gameScreen.ExitPromptResolved += update =>
                {
                    if (!update.HasValue)
                        return;
                    if (update.Value)
                    {
                        GameSave save = gameScreen.CaptureSave();
                        if (save != null)
                            savesService.Save(gameScreen.SavedSlot, save);
                    }
                    ExitRequested?.Invoke();
                    TryExit();
                };
            }
            screens.Set(gameScreen);
        }

        private void ShowSaves(bool saveMode)
        {
            if (savesScreen == null)
            {
                savesScreen = new SavesScreen(GraphicsDevice, Content, savesService,
                    () => gameScreen?.CaptureSave(),
                    () => gameScreen != null && gameScreen.CanCaptureSave);
                savesScreen.Back += ShowGame;
                savesScreen.GameSaved += slot => gameScreen.MarkSaved(slot);
                savesScreen.LoadRequested += save =>
                {
                    gameScreen.RestoreSave(save);
                    ShowGame();
                };
            }
            savesScreen.Open(saveMode);
            screens.Set(savesScreen);
        }

        // автосохранение: всегда в слот 0, перезаписывает предыдущее.
        // Не выполняется, если отключено в настройках, игра окончена/
        // логика занята (CaptureSave вернёт null) или игрок сохранился
        // вручную и после этого не сделал ни одного хода
        private void AutoSaveGame()
        {
            if (gameScreen == null || savesService == null)
                return;
            if (!settings.Current.AutoSaveEnabled)
                return;
            if (gameScreen.HasUnmovedManualSave)
                return;
            GameSave save = gameScreen.CaptureSave();
            if (save != null)
                savesService.AutoSave(save);
        }

        private void ShowSettings()
        {
            if (settingsScreen == null)
            {
                settingsScreen = new SettingsScreen(GraphicsDevice, Content, settings, results);
                settingsScreen.Back += ShowGame;
            }
            screens.Set(settingsScreen);
        }

        // применение настроек к движку: громкость (SoundEffect.MasterVolume),
        // язык интерфейса (Strings.Current) и режим экрана (немедленный
        // переход в fullscreen и обратно)
        private void ApplySettings()
        {
            SoundEffect.MasterVolume = settings.Current.SoundEnabled
                ? settings.Current.Volume / 100f
                : 0f;

            // "auto" - по локали системы (на Web - локаль браузера)
            Strings.Current = Strings.Resolve(settings.Current.Language);

            // на Android режим всегда полноэкранный (immersive в Activity1),
            // на Web канвас и так занимает всю страницу - настройка
            // Fullscreen применяется только на десктопе
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser())
                return;

            if (_graphics.IsFullScreen != settings.Current.Fullscreen)
            {
                // стартовый полноэкранный режим применяется на первом Update
                if (_applyFullscreenOnFirstUpdate)
                    return;
                _graphics.IsFullScreen = settings.Current.Fullscreen;
                _graphics.ApplyChanges();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            EnsureTitle();

            if (_applyFullscreenOnFirstUpdate && !OperatingSystem.IsAndroid()
                && !OperatingSystem.IsBrowser())
            {
                _applyFullscreenOnFirstUpdate = false;
                _graphics.IsFullScreen = true;
                _graphics.ApplyChanges();
            }

            // Esc/Back - по фронту нажатия: на экране игры - подтверждение
            // выхода (диалог в GameScreen), на остальных экранах -
            // возврат на экран игры
            bool backNow = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape);
            if (backNow && !_backWasDown)
                HandleEscape();
            _backWasDown = backNow;

            screens.Update(gameTime, GraphicsDevice.Viewport);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(36, 36, 40));

            screens.Draw(_spriteBatch, GraphicsDevice.Viewport);

            base.Draw(gameTime);
        }
    }
}
