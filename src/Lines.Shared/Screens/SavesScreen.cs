using System;
using Lines.Shared.Input;
using Lines.Shared.Localization;
using Lines.Shared.Rendering;
using Lines.Shared.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Экран сохранённых игр: сетка слотов (9 ручных + автосохранение),
    // всегда в 2 строки по 5 ячеек. В ячейке - превью поля,
    // счёт и дата. Две вкладки: «Загрузить» (тап по занятой ячейке
    // загружает партию) и «Сохранить» (тап по пустой - сохраняет,
    // по занятой - перезапись с подтверждением). Ячейка автосохранения
    // вручную недоступна - она перезаписывается только автосохранением.
    public class SavesScreen : IScreen
    {
        private const float ConfirmSeconds = 4f;

        private readonly SavesService saves;
        private readonly Func<GameSave> captureCurrent; // null-результат - сохранять нельзя
        // дешёвая проверка «можно ли сейчас сохранить» для отрисовки
        // (подсказка/приглушение): CaptureSave копирует всё поле и
        // слишком дорог для вызова каждый кадр
        private readonly Func<bool> canCapture;
        private readonly FontSet fonts;
        private readonly BallRenderer balls;
        private readonly Texture2D pixel;
        private readonly PointerInput pointer = new PointerInput();

        private bool saveMode; // false - загрузка, true - сохранение
        private int armedCell = -1; // ячейка с подтверждением перезаписи
        private float armedAt;
        private float time;

        // кэш подписей ячеек («Счёт N - 30 минут назад»): форматирование
        // даты недешёвое, поэтому пересобираем не каждый кадр, а при
        // открытии экрана, записи в слот и не чаще раза в 30 секунд
        private readonly string[] cellLabels = new string[SavesService.SlotCount];
        private float labelsBuiltAt;
        private bool labelsDirty = true;

        // срабатывает по кнопке «Назад»
        public event Action Back;

        // выбрана партия для загрузки
        public event Action<GameSave> LoadRequested;

        // партия сохранена вручную в слот (GameScreen запоминает его,
        // чтобы при выходе предложить обновить)
        public event Action<int> GameSaved;

        public SavesScreen(GraphicsDevice device, ContentManager content,
            SavesService saves, Func<GameSave> captureCurrent, Func<bool> canCapture)
        {
            this.saves = saves;
            this.captureCurrent = captureCurrent;
            this.canCapture = canCapture;
            fonts = new FontSet(content);
            balls = new BallRenderer(device, content);
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        // открытие экрана в заданном режиме (кнопки «Сохранить»/
        // «Загрузить» на панели игры ведут сразу в нужную вкладку)
        public void Open(bool inSaveMode)
        {
            saveMode = inSaveMode;
            armedCell = -1;
            labelsDirty = true; // подписи ячеек пересоберутся в Draw
        }

        // ScreenManager вызывает при переключении на этот экран
        public void ResetInput() => pointer.Reset();

        public void Update(GameTime gameTime, Viewport viewport)
        {
            pointer.Update();
            time += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (armedCell >= 0 && time - armedAt > ConfirmSeconds)
                armedCell = -1;

            if (!pointer.Click.HasValue)
                return;

            Point click = pointer.Click.Value;
            Layout l = ComputeLayout(viewport);

            if (l.BackButton.Contains(click))
            {
                Back?.Invoke();
                return;
            }
            if (l.LoadTab.Contains(click))
            {
                saveMode = false;
                armedCell = -1;
                return;
            }
            if (l.SaveTab.Contains(click))
            {
                saveMode = true;
                armedCell = -1;
                return;
            }

            for (int i = 0; i < SavesService.SlotCount; i++)
            {
                if (!l.Cells[i].Contains(click))
                    continue;

                if (saveMode)
                    SaveIntoCell(i);
                else if (saves.Slots[i] != null)
                    LoadRequested?.Invoke(saves.Slots[i]);
                return;
            }
        }

        private void SaveIntoCell(int index)
        {
            if (index == SavesService.AutoSlot)
                return; // автосохранение перезаписывает только сама игра

            GameSave current = captureCurrent?.Invoke();
            if (current == null)
                return; // логика занята или игра окончена

            if (saves.Slots[index] == null || armedCell == index)
            {
                saves.Save(index, current);
                armedCell = -1;
                labelsDirty = true; // в слоте новая партия - подпись устарела
                GameSaved?.Invoke(index);
            }
            else
            {
                // ячейка занята - двухшаговое подтверждение перезаписи
                armedCell = index;
                armedAt = time;
            }
        }

        public void Draw(SpriteBatch sb, Viewport viewport)
        {
            Layout l = ComputeLayout(viewport);
            Strings t = Strings.Current;

            DrawTitle(sb, viewport, l, t.SavedGames);
            DrawButton(sb, l.LoadTab, t.Load,
                !saveMode ? UiTheme.Confirm : UiTheme.PanelAlt);
            DrawButton(sb, l.SaveTab, t.Save,
                saveMode ? UiTheme.Confirm : UiTheme.PanelAlt);

            bool canSave = canCapture?.Invoke() ?? false;
            EnsureLabels();
            for (int i = 0; i < SavesService.SlotCount; i++)
                DrawCell(sb, l.Cells[i], i, canSave);

            if (saveMode && !canSave)
                DrawHint(sb, t.SaveHintBusy, viewport, l);
            else if (saveMode)
                DrawHint(sb, t.SaveHintChoose, viewport, l);

            DrawButton(sb, l.BackButton, t.Back, UiTheme.Neutral);
        }

        // --- раскладка ---

        private struct Layout
        {
            public int TitleBand;
            public Rectangle LoadTab, SaveTab;
            public Rectangle[] Cells;
            public int HintY;
            public Rectangle BackButton;
        }

        private static Layout ComputeLayout(Viewport viewport)
        {
            var l = new Layout { Cells = new Rectangle[SavesService.SlotCount] };

            l.TitleBand = Math.Max(52, viewport.Height / 11);
            int tabH = Math.Max(40, viewport.Height / 16);
            int tabW = Math.Min(220, viewport.Width / 3);
            l.LoadTab = new Rectangle(viewport.Width / 2 - tabW - 6, l.TitleBand, tabW, tabH);
            l.SaveTab = new Rectangle(viewport.Width / 2 + 6, l.TitleBand, tabW, tabH);

            int backBand = Math.Max(70, viewport.Height / 8);
            int backW = Math.Min(220, viewport.Width / 2);
            int backH = Math.Min(52, backBand - 20);
            l.BackButton = new Rectangle((viewport.Width - backW) / 2,
                viewport.Height - backBand + (backBand - backH) / 2, backW, backH);

            int areaY = l.TitleBand + tabH + 12;
            int hintH = Math.Max(24, viewport.Height / 28);
            l.HintY = areaY;
            areaY += hintH;
            int areaH = viewport.Height - backBand - areaY - 8;

            // сетка всегда в 2 строки (5 колонок на 10 слотов),
            // даже если экран позволяет больше колонок
            const int gap = 10;
            int finalRows = 2;
            int cols = (SavesService.SlotCount + finalRows - 1) / finalRows;
            int cellW = Math.Min(300, (viewport.Width - gap * (cols + 1)) / cols);
            int cellH = (areaH - gap * (finalRows + 1)) / finalRows;
            cellH = Math.Min(cellH, cellW + 70); // превью квадратное + подпись

            int gridW = cols * cellW + (cols - 1) * gap;
            int x0 = (viewport.Width - gridW) / 2;
            int y0 = areaY + Math.Max(0, (areaH - (finalRows * cellH + (finalRows - 1) * gap)) / 2);

            for (int i = 0; i < SavesService.SlotCount; i++)
            {
                int col = i % cols, row = i / cols;
                l.Cells[i] = new Rectangle(x0 + col * (cellW + gap),
                    y0 + row * (cellH + gap), cellW, cellH);
            }
            return l;
        }

        // --- отрисовка ---

        private void DrawTitle(SpriteBatch sb, Viewport viewport, Layout l, string title)
        {
            SpriteFont sf = fonts.Pick(l.TitleBand * 0.5f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, viewport.Width * 0.8f / sf.MeasureString(title).X);
            Vector2 size = sf.MeasureString(title) * scale;
            sb.DrawString(sf, title,
                new Vector2((viewport.Width - size.X) / 2f, (l.TitleBand - size.Y) / 2f),
                UiTheme.Accent, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // пересборка кэша подписей ячеек: при открытии экрана, записи
        // в слот и не чаще раза в 30 секунд (относительное время в
        // подписи само по себе меняется раз в минуту)
        private void EnsureLabels()
        {
            if (!labelsDirty && time - labelsBuiltAt < 30f)
                return;
            labelsDirty = false;
            labelsBuiltAt = time;
            for (int i = 0; i < SavesService.SlotCount; i++)
            {
                GameSave save = saves.Slots[i];
                // подпись: счёт и относительное время (только символы из
                // шрифта: ASCII + кириллица)
                cellLabels[i] = save == null ? null :
                    string.Format(Strings.Current.SaveCellFormat, save.Score,
                        RelativeTime.Format(save.Date));
            }
        }

        private void DrawCell(SpriteBatch sb, Rectangle cell, int index, bool canSave)
        {
            GameSave save = saves.Slots[index];
            bool isAuto = index == SavesService.AutoSlot;
            // в режиме сохранения ячейка автосохранения недоступна - приглушаем
            bool dimmed = saveMode && isAuto;

            Color back = UiTheme.Panel;
            Color border = armedCell == index ? new Color(200, 80, 70) : UiTheme.BoardFrame;
            if (dimmed)
            {
                back *= 0.6f;
                border *= 0.6f;
            }
            sb.Draw(pixel, cell, border);
            sb.Draw(pixel, new Rectangle(cell.X + 2, cell.Y + 2, cell.Width - 4, cell.Height - 4), back);

            int labelH = Math.Max(20, cell.Height / 6);
            var previewArea = new Rectangle(cell.X + 8, cell.Y + 8,
                cell.Width - 16, cell.Height - labelH - 16);

            if (save == null)
            {
                DrawCenteredIn(sb, Strings.Current.EmptySlot, cell,
                    dimmed ? Color.Gray * 0.6f : Color.Gray);
                return;
            }

            DrawPreview(sb, save, previewArea, dimmed);

            string label = cellLabels[index] ?? "";
            SpriteFont sf = fonts.Pick(labelH * 0.62f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, (cell.Width - 12) / sf.MeasureString(label).X);
            Vector2 size = sf.MeasureString(label) * scale;
            Color labelColor = dimmed ? Color.White * 0.5f : Color.White;
            sb.DrawString(sf, label,
                new Vector2(cell.X + (cell.Width - size.X) / 2f, cell.Bottom - labelH - 4),
                labelColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (isAuto)
            {
                // водяной знак «АВТО»/«AUTO» по центру превью: светло-серый,
                // полупрозрачный, без плашки - не перекрывает шарики
                string auto = Strings.Current.AutoWatermark;
                SpriteFont af = fonts.Pick(previewArea.Height * 0.22f / fonts.Base.LineSpacing,
                    out float ascale);
                ascale = Math.Min(ascale,
                    previewArea.Width * 0.85f / af.MeasureString(auto).X);
                Vector2 autoSize = af.MeasureString(auto) * ascale;
                Color autoColor = new Color(220, 220, 220) * (dimmed ? 0.2f : 0.39f);
                sb.DrawString(af, auto,
                    new Vector2(previewArea.X + (previewArea.Width - autoSize.X) / 2f,
                        previewArea.Y + (previewArea.Height - autoSize.Y) / 2f),
                    autoColor, 0f, Vector2.Zero, ascale, SpriteEffects.None, 0f);
            }

            if (armedCell == index)
                DrawCenteredIn(sb, Strings.Current.OverwritePrompt, previewArea, new Color(255, 150, 140));
        }

        // миниатюра поля 9x9: клетки с линиями сетки и шарики из сохранения
        private void DrawPreview(SpriteBatch sb, GameSave save, Rectangle area, bool dimmed)
        {
            int n = Math.Max(3, save.MapSize);
            int side = Math.Min(area.Width, area.Height);
            var board = new Rectangle(area.X + (area.Width - side) / 2,
                area.Y + (area.Height - side) / 2, side, side);
            Color gridColor = dimmed ? UiTheme.BoardFrame * 0.55f : UiTheme.BoardFrame;
            Color cellBack = dimmed ? UiTheme.CellBack * 0.55f : UiTheme.CellBack;

            // сетка: клетки с 1px зазором, просвечивающий фон - линии
            sb.Draw(pixel, board, gridColor);
            int cw = side / n;
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                    sb.Draw(pixel, new Rectangle(board.X + x * cw, board.Y + y * cw,
                        cw - 1, cw - 1), cellBack);

            int inset = Math.Max(1, cw / 8);
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                {
                    if (save.Balls == null || y * n + x >= save.Balls.Length)
                        continue;
                    int color = save.Balls[y * n + x];
                    if (color <= 0 || color > BallRenderer.ColorCount)
                        continue;
                    var dest = new Rectangle(board.X + x * cw + inset,
                        board.Y + y * cw + inset,
                        Math.Max(2, cw - inset * 2), Math.Max(2, cw - inset * 2));
                    balls.Draw(sb, color, dest);
                }
        }

        // относительное время - общий хелпер RelativeTime (Screens/RelativeTime.cs)

        private void DrawHint(SpriteBatch sb, string text, Viewport viewport, Layout l)
        {
            SpriteFont sf = fonts.Pick(0.7f, out float scale);
            scale = Math.Min(scale, viewport.Width * 0.9f / sf.MeasureString(text).X);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text, new Vector2((viewport.Width - size.X) / 2f, l.HintY),
                UiTheme.FooterText, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawCenteredIn(SpriteBatch sb, string text, Rectangle area, Color color)
        {
            SpriteFont sf = fonts.Pick(area.Height * 0.12f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, area.Width * 0.85f / sf.MeasureString(text).X);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text,
                new Vector2(area.X + (area.Width - size.X) / 2f, area.Y + (area.Height - size.Y) / 2f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawButton(SpriteBatch sb, Rectangle rect, string text, Color back)
        {
            sb.Draw(pixel, rect, back);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.White * 0.25f);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), Color.Black * 0.3f);

            SpriteFont sf = fonts.Pick(rect.Height * 0.45f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, rect.Width * 0.9f / sf.MeasureString(text).X);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text, new Vector2(rect.X + (rect.Width - size.X) / 2f,
                rect.Y + (rect.Height - size.Y) / 2f), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
