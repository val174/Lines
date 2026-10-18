using System;
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
        private static readonly string[] iconNames = { "btn_new_game", "btn_save", "btn_load",
            "btn_settings", "btn_records", "btn_exit" };

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

        // нижняя панель с кнопками-иконками (в обеих ориентациях)
        public void DrawStatusBar(SpriteBatch sb, BoardLayout layout)
        {
            sb.Draw(pixel, layout.StatusBarRect, UiTheme.Panel);
            foreach (PanelButton b in Buttons)
                DrawIconButton(sb, ButtonRect(layout, b), icons[(int)b]);
        }

        private void DrawIconButton(SpriteBatch sb, Rectangle btn, Texture2D icon)
        {
            sb.Draw(pixel, btn, new Color(64, 58, 48));
            sb.Draw(pixel, new Rectangle(btn.X, btn.Y, btn.Width, 2), Color.White * 0.25f);
            sb.Draw(pixel, new Rectangle(btn.X, btn.Bottom - 2, btn.Width, 2), Color.Black * 0.3f);
            Rectangle dest = btn;
            dest.Inflate(-btn.Width / 10, -btn.Height / 10);
            sb.Draw(icon, dest, Color.White);
        }

        // квадратные кнопки-иконки делят нижнюю панель с равными зазорами
        public static Rectangle ButtonRect(BoardLayout layout, PanelButton button)
        {
            int index = (int)button;
            int count = Buttons.Length;
            Rectangle bar = layout.StatusBarRect;
            int h = Math.Max(34, bar.Height * 7 / 10);
            int w = h;
            int gap = (bar.Width - w * count) / (count + 1);
            if (gap < 4)
            {
                gap = 4;
                w = (bar.Width - gap * (count + 1)) / count;
                h = w;
            }
            return new Rectangle(bar.X + gap + index * (w + gap),
                bar.Y + (bar.Height - h) / 2, w, h);
        }
    }
}
