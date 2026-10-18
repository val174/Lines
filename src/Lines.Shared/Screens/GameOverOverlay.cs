using System;
using System.Collections.Generic;
using Lines.Shared.Localization;
using Lines.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Отрисовка оверлеев конца игры и первого запуска: затемнение,
    // заголовки, поле ввода имени, таблица рекордов. Только рисование -
    // состояние (этап флоу, имя, кэш строк таблицы) живёт в GameScreen
    // и передаётся параметрами; клики разбирает тоже GameScreen
    // (по той же раскладке ComputeLayout).
    internal class GameOverOverlay
    {
        private readonly FontSet fonts;
        private readonly Texture2D pixel; // 1x1 белый - для заливок

        public GameOverOverlay(FontSet fonts, Texture2D pixel)
        {
            this.fonts = fonts;
            this.pixel = pixel;
        }

        // отформатированная строка таблицы рекордов (кэширует GameScreen)
        public struct ResultRow
        {
            public string Pos, Name, Score, Date;
        }

        // раскладка диалога конца игры (общая для отрисовки и кликов)
        public struct Layout
        {
            public int TitleY, ScoreY, PromptY, ContentY;
            public float TableX, TableWidth, TableRowHeight;
            public Rectangle NameField, SaveButton, SkipButton;
        }

        public static Layout ComputeLayout(Viewport viewport)
        {
            // ширина и высота строк - от размера экрана (на телефоне
            // фиксированные 420px/30px выглядят слишком мелко)
            int w = Math.Min((int)(viewport.Width * 0.92f), (int)(viewport.Height * 0.68f));
            int x = (viewport.Width - w) / 2;

            var l = new Layout();
            l.TitleY = viewport.Height / 9;
            l.ScoreY = l.TitleY + 48;
            l.TableRowHeight = Math.Clamp(viewport.Height / 26f, 30f, 72f);
            // заголовок «Рекорды» - высоко, между ним и таблицей
            // отступ в две строки таблицы
            l.PromptY = l.TitleY + 44;
            l.ContentY = l.PromptY + 50 + (int)(2 * l.TableRowHeight);
            l.TableX = x;
            l.TableWidth = w;
            l.NameField = new Rectangle(x, l.ContentY, w, 44);
            l.SaveButton = new Rectangle(x, l.ContentY + 56, w / 2 - 6, 40);
            l.SkipButton = new Rectangle(x + w / 2 + 6, l.ContentY + 56, w / 2 - 6, 40);
            return l;
        }

        // конец игры: либо ввод имени (счёт попал в топ), либо таблица
        // рекордов; nativeDialog - имя спрашивает платформенный диалог
        // (Android), тогда экранного поля ввода нет.
        // cursorPhase - фаза [0, 1) мигания курсора в поле имени
        public void DrawGameOver(SpriteBatch sb, Viewport viewport, bool enterName,
            string scoreText, string playerName, float cursorPhase, bool nativeDialog,
            IReadOnlyList<ResultRow> rows, int highlight)
        {
            sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), UiTheme.OverlayDim);

            Layout l = ComputeLayout(viewport);

            if (enterName)
            {
                OverlayDialogs.DrawCentered(sb, fonts, Strings.Current.GameOver, viewport, l.TitleY, 1.6f, Color.White);
                OverlayDialogs.DrawCentered(sb, fonts, scoreText, viewport, l.ScoreY, 1.2f,
                    new Color(230, 220, 180));
                DrawNameEntry(sb, l, viewport, Strings.Current.NewRecordEnterName, playerName, cursorPhase, nativeDialog);
            }
            else
            {
                // на экране результатов заголовок «Рекорды» рисует сама
                // таблица - высоко и без дублей
                DrawResultsTable(sb, l, viewport, Strings.Current.ClickForNewGame, rows, highlight);
            }
        }

        // первый запуск: «как вас зовут?» поверх игрового поля;
        // имя сохраняется в настройках и предзаполняет поле рекорда
        public void DrawAskName(SpriteBatch sb, Viewport viewport, string playerName,
            float cursorPhase, bool nativeDialog)
        {
            sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), UiTheme.OverlayDim);
            Layout l = ComputeLayout(viewport);
            OverlayDialogs.DrawCentered(sb, fonts, Strings.Current.Welcome, viewport, l.TitleY, 1.6f, Color.White);
            if (nativeDialog)
            {
                // на Android имя спрашивает нативный диалог поверх игры
                OverlayDialogs.DrawCentered(sb, fonts, Strings.Current.AskName, viewport, l.PromptY, 1.0f,
                    UiTheme.Accent);
                return;
            }
            DrawNameEntry(sb, l, viewport, Strings.Current.AskName, playerName, cursorPhase, nativeDialog);
        }

        // таблица рекордов поверх игры (кнопка «Рекорды» на панели);
        // любой клик закрывает - см. GameScreen.HandleClick
        public void DrawRecords(SpriteBatch sb, Viewport viewport,
            IReadOnlyList<ResultRow> rows, int highlight)
        {
            sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), UiTheme.OverlayDim);
            Layout l = ComputeLayout(viewport);
            DrawResultsTable(sb, l, viewport, Strings.Current.ClickToReturn, rows, highlight);
        }

        // ввод имени: поле + кнопки «Сохранить»/«Пропустить»;
        // набор - через TextInput (раскладка, кириллица), Enter - сохранить.
        // При nativeDialog ввод идёт в нативном диалоге - здесь
        // показываем только заголовок
        private void DrawNameEntry(SpriteBatch sb, Layout l, Viewport viewport, string prompt,
            string playerName, float cursorPhase, bool nativeDialog)
        {
            OverlayDialogs.DrawCentered(sb, fonts, prompt, viewport, l.PromptY, 1.0f,
                UiTheme.Accent);

            if (nativeDialog)
                return; // имя спрашивает нативный диалог поверх игры

            sb.Draw(pixel, l.NameField, new Color(245, 240, 225));
            string shown = playerName + ((int)(cursorPhase * 2) % 2 == 0 ? "_" : " ");
            // 10f - иначе деление целочисленное и scale обнуляется
            float baseScale = Math.Min(1.2f, (l.NameField.Height - 10f) / fonts.Base.LineSpacing);
            SpriteFont sf = fonts.Pick(baseScale, out float scale);
            Vector2 textSize = sf.MeasureString(shown) * scale;
            var textPos = new Vector2(l.NameField.X + 10,
                l.NameField.Y + (l.NameField.Height - textSize.Y) / 2f);
            sb.DrawString(sf, shown, textPos, new Color(40, 38, 34), 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f);

            OverlayDialogs.DrawMenuButton(sb, pixel, fonts, l.SaveButton, Strings.Current.Save, UiTheme.Confirm);
            OverlayDialogs.DrawMenuButton(sb, pixel, fonts, l.SkipButton, Strings.Current.Skip, UiTheme.Neutral);
        }

        // таблица рекордов: топ-10, свежая запись подсвечена;
        // строки уже отформатированы (кэш GameScreen)
        private void DrawResultsTable(SpriteBatch sb, Layout l, Viewport viewport, string footer,
            IReadOnlyList<ResultRow> rows, int highlight)
        {
            OverlayDialogs.DrawCentered(sb, fonts, Strings.Current.Records, viewport, l.PromptY, 1.45f,
                UiTheme.Accent);

            float rowH = l.TableRowHeight;
            SpriteFont sf = fonts.Pick(rowH * 0.72f / fonts.Base.LineSpacing, out float scale);
            float xPos = l.TableX, xName = l.TableX + 60,
                xScore = l.TableX + l.TableWidth * 0.50f,
                xDate = l.TableX + l.TableWidth * 0.68f;
            for (int i = 0; i < rows.Count; i++)
            {
                float y = l.ContentY + i * rowH;
                if (i == highlight)
                    sb.Draw(pixel, new Rectangle((int)l.TableX - 8, (int)y - 2,
                            (int)l.TableWidth + 16, (int)rowH),
                        UiTheme.Confirm * 0.5f);
                Color color = i == highlight ? new Color(255, 240, 180) : Color.White;
                // дата - относительное время; у старых записей даты
                // может не быть - тогда пусто
                sb.DrawString(sf, rows[i].Pos, new Vector2(xPos, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                sb.DrawString(sf, rows[i].Name, new Vector2(xName, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                sb.DrawString(sf, rows[i].Score, new Vector2(xScore, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                sb.DrawString(sf, rows[i].Date, new Vector2(xDate, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            OverlayDialogs.DrawCentered(sb, fonts, footer, viewport, viewport.Height - 50, 0.9f,
                UiTheme.FooterText);
        }
    }
}
