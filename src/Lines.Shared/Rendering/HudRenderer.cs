using System;
using Lines.Shared.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Rendering
{
    // кнопки нижней панели игрового экрана; порядок значений =
    // порядок кнопок слева направо (по нему же грузятся иконки
    // и обрабатываются клики в GameScreen)
    internal enum PanelButton
    {
        NewGame,
        Undo,
        Save,
        Load,
        Settings,
        Records,
        Exit,
    }

    // Верхняя панель (счёт + предпросмотр «следующих» шариков) и нижняя
    // панель с кнопками-иконками (спрайты Content/Buttons/btn_*).
    internal class HudRenderer
    {
        // все кнопки в порядке слева направо - по этому массиву грузятся
        // иконки, рисуется панель и проверяются клики
        public static readonly PanelButton[] Buttons =
            (PanelButton[])Enum.GetValues(typeof(PanelButton));

        // иконка каждой кнопки - по индексу значения PanelButton
        private static readonly string[] iconNames = { "btn_new_game", "btn_undo", "btn_save",
            "btn_load", "btn_settings", "btn_records", "btn_exit" };

        // кнопки, видимые в данной раскладке: в портрете «Загрузить»
        // скрыта (узкая строка иконок; загрузка остаётся через
        // автосохранение и диалог «Новая игра»)
        public static PanelButton[] VisibleButtons(BoardLayout layout)
        {
            if (!layout.IsPortrait)
                return Buttons;
            var visible = new PanelButton[Buttons.Length - 1];
            int j = 0;
            foreach (PanelButton b in Buttons)
                if (b != PanelButton.Load)
                    visible[j++] = b;
            return visible;
        }

        private readonly Texture2D pixel; // 1x1 белый - для заливок
        private readonly FontSet fonts;
        private readonly BallRenderer balls;
        private readonly Texture2D[] icons = new Texture2D[Buttons.Length];

        public HudRenderer(ContentManager content, Texture2D pixel, FontSet fonts, BallRenderer balls)
        {
            this.pixel = pixel;
            this.fonts = fonts;
            this.balls = balls;
            foreach (PanelButton b in Buttons)
                icons[(int)b] = content.Load<Texture2D>("Buttons/" + iconNames[(int)b]);
        }

        // верхняя панель: счёт (уже отформатирован) и предпросмотр
        // «следующих» шариков
        public void DrawPanel(SpriteBatch sb, BoardLayout layout, string scoreText, int[] nextColors)
        {
            Rectangle panel = layout.PanelRect;
            sb.Draw(pixel, panel, UiTheme.Panel);

            float textScale = Math.Max(0.5f, panel.Height / 48f);
            SpriteFont sf = fonts.Pick(textScale, out float scale);
            Vector2 textSize = sf.MeasureString(scoreText) * scale;

            // предпросмотр «следующих» шариков: в ландшафте - по центру
            // панели, в портрете - сразу после счёта (кнопки уехали вниз)
            int ballSize = (int)(panel.Height * 0.7f);
            int gap = Math.Max(4, ballSize / 4);
            int x0;
            if (layout.IsPortrait)
            {
                // в портрете панель высокая, а ширина ограничена -
                // ужимаем текст и шарики, чтобы всё влезло
                float maxTextW = panel.Width * 0.38f;
                if (textSize.X > maxTextW)
                {
                    scale *= maxTextW / textSize.X;
                    textSize = sf.MeasureString(scoreText) * scale;
                }
                int avail = panel.Width - 32 - (int)textSize.X - gap * 2;
                ballSize = Math.Max(12, Math.Min(ballSize, (avail - 2 * gap) / 3));
                gap = Math.Max(4, ballSize / 4);
                x0 = 16 + (int)textSize.X + gap * 2;
            }
            else
            {
                int totalWidth = 3 * ballSize + 2 * gap;
                x0 = panel.X + (panel.Width - totalWidth) / 2;
            }

            sb.DrawString(sf, scoreText, new Vector2(16, (panel.Height - textSize.Y) / 2f),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            int y0 = (panel.Height - ballSize) / 2;
            for (int i = 0; i < 3; i++)
            {
                Rectangle dest = new Rectangle(x0 + i * (ballSize + gap), y0, ballSize, ballSize);
                sb.Draw(pixel, dest, UiTheme.PanelAlt);
                if (nextColors[i] > 0)
                {
                    dest.Inflate(-2, -2);
                    balls.Draw(sb, nextColors[i], dest);
                }
            }
        }

        // нижняя панель с кнопками: в портрете - квадратные иконки,
        // в ландшафте - широкие кнопки с иконкой слева и подписью справа
        // (две группы с увеличенным зазором между ними). Кнопка отмены
        // приглушается, когда отменять нечего (undoEnabled = false)
        public void DrawStatusBar(SpriteBatch sb, BoardLayout layout, bool undoEnabled)
        {
            sb.Draw(pixel, layout.StatusBarRect, UiTheme.Panel);
            foreach (PanelButton b in VisibleButtons(layout))
            {
                Rectangle rect = ButtonRect(layout, b);
                bool enabled = b != PanelButton.Undo || undoEnabled;
                if (layout.IsPortrait)
                    DrawIconButton(sb, rect, icons[(int)b], enabled);
                else
                    DrawWideButton(sb, rect, icons[(int)b], Label(b), enabled);
            }
        }

        // подписи кнопок нижней панели (ландшафт) - из Strings
        private static string Label(PanelButton button)
        {
            Strings t = Strings.Current;
            switch (button)
            {
                case PanelButton.NewGame: return t.NewGame;
                case PanelButton.Undo: return t.Undo;
                case PanelButton.Save: return t.Save;
                case PanelButton.Load: return t.Load;
                case PanelButton.Settings: return t.Settings;
                case PanelButton.Records: return t.Records;
                default: return t.Exit;
            }
        }

        // широкая кнопка ландшафтной панели: иконка слева (~62% высоты),
        // подпись справа с подбором шрифта под оставшуюся ширину
        private void DrawWideButton(SpriteBatch sb, Rectangle btn, Texture2D icon, string label,
            bool enabled = true)
        {
            Color tint = enabled ? Color.White : Color.White * 0.35f;
            sb.Draw(pixel, btn, new Color(64, 58, 48));
            sb.Draw(pixel, new Rectangle(btn.X, btn.Y, btn.Width, 2), Color.White * 0.25f);
            sb.Draw(pixel, new Rectangle(btn.X, btn.Bottom - 2, btn.Width, 2), Color.Black * 0.3f);

            int iconSize = (int)(btn.Height * 0.62f);
            int pad = Math.Max(6, btn.Height / 10);
            int iconX = btn.X + pad;
            sb.Draw(icon, new Rectangle(iconX, btn.Y + (btn.Height - iconSize) / 2,
                iconSize, iconSize), tint);

            int textX = iconX + iconSize + pad;
            float textW = btn.Right - textX - pad;
            SpriteFont sf = fonts.Pick(btn.Height * 0.34f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, textW / Math.Max(1f, sf.MeasureString(label).X));
            Vector2 size = sf.MeasureString(label) * scale;
            sb.DrawString(sf, label, new Vector2(textX, btn.Y + (btn.Height - size.Y) / 2f),
                tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawIconButton(SpriteBatch sb, Rectangle btn, Texture2D icon, bool enabled = true)
        {
            sb.Draw(pixel, btn, new Color(64, 58, 48));
            sb.Draw(pixel, new Rectangle(btn.X, btn.Y, btn.Width, 2), Color.White * 0.25f);
            sb.Draw(pixel, new Rectangle(btn.X, btn.Bottom - 2, btn.Width, 2), Color.Black * 0.3f);
            Rectangle dest = btn;
            dest.Inflate(-btn.Width / 10, -btn.Height / 10);
            sb.Draw(icon, dest, enabled ? Color.White : Color.White * 0.35f);
        }

        // прямоугольник кнопки на нижней панели - единый источник
        // раскладки для отрисовки и hit-test (GameScreen.HandleClick).
        // Портрет: квадратные кнопки-иконки с равными зазорами.
        // Ландшафт: широкие кнопки во всю ширину, две группы
        // (игровые | системные), между группами зазор x4.
        // Раскладка считается по видимым кнопкам (в портрете «Загрузить»
        // скрыта) - для скрытой кнопки возвращается пустой прямоугольник
        public static Rectangle ButtonRect(BoardLayout layout, PanelButton button)
        {
            PanelButton[] visible = VisibleButtons(layout);
            int index = Array.IndexOf(visible, button);
            if (index < 0)
                return Rectangle.Empty;
            int count = visible.Length;
            Rectangle bar = layout.StatusBarRect;

            if (!layout.IsPortrait)
            {
                int g = Math.Max(6, bar.Height / 12);
                int marginY = Math.Max(4, bar.Height / 12);
                int h = bar.Height - marginY * 2;
                int groupGap = g * 4;
                int w = (bar.Width - g * 2 - g * (count - 1) - (groupGap - g)) / count;
                int x = bar.X + g + index * (w + g) + (index >= 3 ? groupGap - g : 0);
                return new Rectangle(x, bar.Y + marginY, w, h);
            }

            int ph = Math.Max(34, bar.Height * 7 / 10);
            int w0 = ph;
            int gap = (bar.Width - w0 * count) / (count + 1);
            if (gap < 4)
            {
                gap = 4;
                w0 = (bar.Width - gap * (count + 1)) / count;
                ph = w0;
            }
            return new Rectangle(bar.X + gap + index * (w0 + gap),
                bar.Y + (bar.Height - ph) / 2, w0, ph);
        }
    }
}
