using System;
using Lines.Shared.Input;
using Lines.Shared.Localization;
using Lines.Shared.Rendering;
using Lines.Shared.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Экран настроек: звук (вкл/выкл), громкость (уровни 0-10: кнопки
    // -/+ и тап по сегменту шкалы, без перетаскивания), следы шариков
    // (вкл/выкл), режим экрана (окно / полный экран),
    // очистка рекордов (двухшаговое подтверждение), кнопка «Назад».
    // Все изменения применяются сразу и сохраняются
    // через SettingsService (событие Changed - у LinesGame и GameScreen).
    // Контролы рисуются программно - работают и мышью, и тачем.
    // На Android и Web строки режима экрана нет - там всегда полный экран
    // (immersive / канвас на всю страницу).
    public class SettingsScreen : IScreen
    {
        private static readonly bool NoScreenMode =
            OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser();
        private static readonly int RowCount = NoScreenMode ? 6 : 7;
        private static readonly bool ShowScreenMode = !NoScreenMode;

        private readonly SettingsService settings;
        private readonly ResultsService results;
        private readonly FontSet fonts;
        private readonly SoundEffect sndSample; // звук-пример громкости
        private readonly Texture2D pixel;
        private readonly PointerInput pointer = new PointerInput();

        // двухшаговая очистка рекордов: первый клик - «Подтвердить?»,
        // повторный в течение 4 секунд - очистка
        private const float ClearConfirmSeconds = 4f;
        private bool clearArmed;
        private float clearArmedAt;
        private float time;

        // срабатывает по кнопке «Назад»
        public event Action Back;

        public SettingsScreen(GraphicsDevice device, ContentManager content,
            SettingsService settings, ResultsService results)
        {
            this.settings = settings;
            this.results = results;
            fonts = new FontSet(content);
            sndSample = content.Load<SoundEffect>("Sounds/select");
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        // ScreenManager вызывает при переключении на этот экран
        public void ResetInput() => pointer.Reset();

        public void Update(GameTime gameTime, Viewport viewport)
        {
            pointer.Update();
            time += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (clearArmed && time - clearArmedAt > ClearConfirmSeconds)
                clearArmed = false;

            Layout l = ComputeLayout(viewport);

            // кнопки-переключатели - по клику
            if (pointer.Click.HasValue)
            {
                Point click = pointer.Click.Value;
                if (l.SoundRow.Contains(click))
                    settings.Update(s => s.SoundEnabled = !s.SoundEnabled);
                else if (l.TrailsRow.Contains(click))
                    settings.Update(s => s.ShowBallTrails = !s.ShowBallTrails);
                else if (l.AutoSaveRow.Contains(click))
                    settings.Update(s => s.AutoSaveEnabled = !s.AutoSaveEnabled);
                else if (l.LanguageRow.Contains(click))
                    settings.Update(s => s.Language = NextLanguage(s.Language));
                else if (ShowScreenMode && l.WindowButton.Contains(click))
                    settings.Update(s => s.Fullscreen = false);
                else if (ShowScreenMode && l.FullscreenButton.Contains(click))
                    settings.Update(s => s.Fullscreen = true);
                else if (l.ClearRow.Contains(click))
                {
                    if (clearArmed)
                    {
                        results.Clear();
                        clearArmed = false;
                    }
                    else
                    {
                        clearArmed = true;
                        clearArmedAt = time;
                    }
                }
                else if (l.BackButton.Contains(click))
                    Back?.Invoke();
                // громкость: -/+ шаг на уровень, тап по сегменту - сразу
                // уровень (перетаскивания нет - на таче оно «зависало»)
                else if (l.VolumeMinus.Contains(click))
                    SetVolumeLevel(VolumeLevel() - 1);
                else if (l.VolumePlus.Contains(click))
                    SetVolumeLevel(VolumeLevel() + 1);
                else if (l.VolumeSegments.Contains(click))
                {
                    int seg = (click.X - l.VolumeSegments.X) * 10 / l.VolumeSegments.Width;
                    SetVolumeLevel(seg + 1);
                }
            }
        }

        // текущий уровень громкости 0-10 (Volume хранит 0-100%)
        private int VolumeLevel() =>
            Math.Clamp((int)Math.Round(settings.Current.Volume / 10.0), 0, 10);

        private void SetVolumeLevel(int level)
        {
            level = Math.Clamp(level, 0, 10);
            if (level == VolumeLevel())
                return;
            settings.Update(s => s.Volume = level * 10);
            // звук-пример: сразу слышно новую громкость
            if (settings.Current.SoundEnabled)
                sndSample.Play();
        }

        public void Draw(SpriteBatch sb, Viewport viewport)
        {
            Layout l = ComputeLayout(viewport);
            Strings t = Strings.Current;

            DrawTitle(sb, viewport, t.Settings);
            DrawToggleRow(sb, l.SoundRow, l.S, t.Sound, settings.Current.SoundEnabled);
            DrawVolumeRow(sb, l, string.Format(t.VolumeFormat, settings.Current.Volume));
            DrawToggleRow(sb, l.TrailsRow, l.S, t.BallTrails, settings.Current.ShowBallTrails);
            DrawToggleRow(sb, l.AutoSaveRow, l.S, t.AutoSave, settings.Current.AutoSaveEnabled);
            DrawLanguageRow(sb, l, t);
            if (ShowScreenMode)
                DrawModeRow(sb, l, t);
            DrawClearRow(sb, l, t);
            DrawButton(sb, l.BackButton, t.Back, UiTheme.Neutral, Color.White);
        }

        // --- раскладка ---

        // масштаб интерфейса - общий хелпер UiTheme.Scale (от меньшей
        // стороны экрана; в десктопном окне ~800x571 s ~= 1)
        private static float UiScale(Viewport viewport) => UiTheme.Scale(viewport);

        private struct Layout
        {
            public float S; // масштаб для мелких элементов (ползунок и т.п.)
            public Rectangle SoundRow;
            public Rectangle VolumeRow;
            // громкость: уровни 0-10, кнопки - и + по краям, между ними
            // шкала из 10 сегментов (тап по сегменту = сразу уровень)
            public Rectangle VolumeMinus;
            public Rectangle VolumePlus;
            public Rectangle VolumeSegments;
            public Rectangle TrailsRow;
            public Rectangle AutoSaveRow;
            public Rectangle LanguageRow;
            public Rectangle WindowButton;
            public Rectangle FullscreenButton;
            public Rectangle ClearRow;
            public Rectangle BackButton;
        }

        private static Layout ComputeLayout(Viewport viewport)
        {
            float s = UiScale(viewport);
            int titleBand = Math.Max((int)(60 * s), viewport.Height / 6);
            int bottomBand = Math.Max((int)(70 * s), viewport.Height / 6);
            int areaH = viewport.Height - titleBand - bottomBand;
            int rowH = Math.Min((int)(72 * s), Math.Max((int)(44 * s), areaH / (RowCount + 1)));
            int colW = Math.Min((int)(560 * s), (int)(viewport.Width * 0.92f));
            int x = (viewport.Width - colW) / 2;
            int y = titleBand + Math.Max(0, (areaH - rowH * RowCount) / 2);

            Layout l;
            l.S = s;
            l.SoundRow = new Rectangle(x, y, colW, rowH);
            l.VolumeRow = new Rectangle(x, y + rowH, colW, rowH);
            // зона громкости - точно по ширине кнопок остальных строк:
            // кнопки -/+ по краям, между ними шкала сегментов
            Rectangle zone = RowButton(l.VolumeRow, s);
            int pmW = Math.Min((int)(56 * s), zone.Width / 4);
            int vgap = Math.Max(4, (int)(8 * s));
            l.VolumeMinus = new Rectangle(zone.X, zone.Y, pmW, zone.Height);
            l.VolumePlus = new Rectangle(zone.Right - pmW, zone.Y, pmW, zone.Height);
            l.VolumeSegments = new Rectangle(zone.X + pmW + vgap,
                zone.Y + zone.Height / 6, zone.Width - 2 * (pmW + vgap), zone.Height * 2 / 3);
            l.TrailsRow = new Rectangle(x, y + 2 * rowH, colW, rowH);
            l.AutoSaveRow = new Rectangle(x, y + 3 * rowH, colW, rowH);
            l.LanguageRow = new Rectangle(x, y + 4 * rowH, colW, rowH);

            int modeY = y + 5 * rowH;
            int half = (colW - 8) / 2;
            l.WindowButton = new Rectangle(x, modeY, half, rowH);
            l.FullscreenButton = new Rectangle(x + half + 8, modeY, half, rowH);

            int clearIdx = ShowScreenMode ? 6 : 5;
            l.ClearRow = new Rectangle(x, y + clearIdx * rowH, colW, rowH);

            int backW = Math.Min((int)(220 * s), colW / 2);
            int backH = Math.Min((int)(56 * s), bottomBand - 20);
            l.BackButton = new Rectangle((viewport.Width - backW) / 2,
                viewport.Height - bottomBand + (bottomBand - backH) / 2, backW, backH);
            return l;
        }

        // --- отрисовка контролов ---

        private void DrawTitle(SpriteBatch sb, Viewport viewport, string title)
        {
            int bandHeight = Math.Max((int)(60 * UiScale(viewport)), viewport.Height / 6);
            SpriteFont sf = fonts.Pick(bandHeight * 0.55f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, viewport.Width * 0.8f / sf.MeasureString(title).X);
            Vector2 size = sf.MeasureString(title) * scale;
            sb.DrawString(sf, title,
                new Vector2((viewport.Width - size.X) / 2f, (bandHeight - size.Y) / 2f),
                UiTheme.Accent, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // единый размер кнопок во всех строках (Вкл/Выкл, язык,
        // «Очистить»/«Подтвердить?»): одна ширина, чтобы длинный текст
        // «Confirm?» влезал без сильного ужатия (clamp в DrawButton)
        private static Rectangle RowButton(Rectangle row, float s)
        {
            int btnW = Math.Min((int)(260 * s), row.Width / 2);
            return new Rectangle(row.Right - btnW, row.Y + row.Height / 6, btnW, row.Height * 2 / 3);
        }

        // строка-переключатель: подпись слева, кнопка со состоянием справа
        private void DrawToggleRow(SpriteBatch sb, Rectangle row, float s, string label, bool enabled)
        {
            SpriteFont sf = fonts.Pick(row.Height * 0.4f / fonts.Base.LineSpacing, out float scale);
            sb.DrawString(sf, label, new Vector2(row.X, row.Y + (row.Height - sf.LineSpacing * scale) / 2f),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            Strings t = Strings.Current;
            DrawButton(sb, RowButton(row, s), enabled ? t.On : t.Off,
                enabled ? UiTheme.Confirm : UiTheme.Danger,
                Color.White);
        }

        private void DrawVolumeRow(SpriteBatch sb, Layout l, string label)
        {
            SpriteFont sf = fonts.Pick(l.VolumeRow.Height * 0.4f / fonts.Base.LineSpacing, out float scale);
            sb.DrawString(sf, label,
                new Vector2(l.VolumeRow.X, l.VolumeRow.Y + (l.VolumeRow.Height - sf.LineSpacing * scale) / 2f),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            DrawButton(sb, l.VolumeMinus, "-", UiTheme.PanelAlt, Color.White);
            DrawButton(sb, l.VolumePlus, "+", UiTheme.PanelAlt, Color.White);

            // шкала из 10 сегментов: заполненные (уровень) - жёлтые
            int level = VolumeLevel();
            int gap = Math.Max(2, l.VolumeSegments.Height / 10);
            int segW = (l.VolumeSegments.Width - gap * 9) / 10;
            for (int i = 0; i < 10; i++)
            {
                var seg = new Rectangle(l.VolumeSegments.X + i * (segW + gap),
                    l.VolumeSegments.Y, segW, l.VolumeSegments.Height);
                sb.Draw(pixel, seg, i < level ? new Color(225, 205, 40) : new Color(50, 48, 42));
            }
        }

        // язык интерфейса: циклическая кнопка Авто/Русский/English
        // (применяется сразу - LinesGame по событию Changed ставит
        // Strings.Current)
        private static string NextLanguage(string lang) =>
            lang == "auto" ? "ru" : lang == "ru" ? "en" : "auto";

        private string LanguageLabel(Strings t) =>
            settings.Current.Language == "ru" ? "Русский"
                : settings.Current.Language == "en" ? "English"
                : t.LanguageAuto;

        private void DrawLanguageRow(SpriteBatch sb, Layout l, Strings t)
        {
            SpriteFont sf = fonts.Pick(l.LanguageRow.Height * 0.4f / fonts.Base.LineSpacing, out float scale);
            sb.DrawString(sf, t.Language, new Vector2(l.LanguageRow.X,
                l.LanguageRow.Y + (l.LanguageRow.Height - sf.LineSpacing * scale) / 2f),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            DrawButton(sb, RowButton(l.LanguageRow, l.S), LanguageLabel(t), UiTheme.PanelAlt, Color.White);
        }

        // режим экрана: сегмент «Окно» / «Полный экран»
        private void DrawModeRow(SpriteBatch sb, Layout l, Strings t)
        {
            DrawButton(sb, l.WindowButton, t.WindowMode,
                !settings.Current.Fullscreen ? UiTheme.Confirm : UiTheme.PanelAlt, Color.White);
            DrawButton(sb, l.FullscreenButton, t.FullscreenMode,
                settings.Current.Fullscreen ? UiTheme.Confirm : UiTheme.PanelAlt, Color.White);
        }

        // очистка рекордов: первый клик включает «Подтвердить?»,
        // повторный (в течение ClearConfirmSeconds) - очищает таблицу
        private void DrawClearRow(SpriteBatch sb, Layout l, Strings t)
        {
            SpriteFont sf = fonts.Pick(l.ClearRow.Height * 0.4f / fonts.Base.LineSpacing, out float scale);
            sb.DrawString(sf, t.Records, new Vector2(l.ClearRow.X,
                l.ClearRow.Y + (l.ClearRow.Height - sf.LineSpacing * scale) / 2f),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            DrawButton(sb, RowButton(l.ClearRow, l.S), clearArmed ? t.ConfirmPrompt : t.Clear,
                clearArmed ? UiTheme.Danger : UiTheme.PanelAlt, Color.White);
        }

        private void DrawButton(SpriteBatch sb, Rectangle rect, string text, Color back, Color fore)
        {
            sb.Draw(pixel, rect, back);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.White * 0.25f);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), Color.Black * 0.3f);

            SpriteFont sf = fonts.Pick(rect.Height * 0.45f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, rect.Width * 0.9f / sf.MeasureString(text).X);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text, new Vector2(rect.X + (rect.Width - size.X) / 2f,
                rect.Y + (rect.Height - size.Y) / 2f), fore, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
