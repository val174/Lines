using System;
using Lines.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Общий каркас диалогов поверх игрового поля: затемнение экрана,
    // вопрос по центру и ряд кнопок (2 или 3). Раскладка (PromptLayout)
    // общая для отрисовки и обработки кликов - экран вычисляет её
    // той же функцией и передаёт и в Draw, и в обработчик клика.
    internal static class OverlayDialogs
    {
        // раскладка кнопок диалога: Yes/No (+ опциональный Cancel)
        // и вертикаль строки вопроса
        internal struct PromptLayout
        {
            public Rectangle Yes, No, Cancel;
            public int QuestionY;
        }

        // диалог с тремя кнопками: вопрос перед выходом
        // («обновить сохранение?») и диалог новой игры
        internal static PromptLayout ComputeThreeButtonLayout(Viewport viewport)
        {
            int btnH = Math.Max(44, viewport.Height / 14);
            int btnW = Math.Max(110, Math.Min(200, (viewport.Width - 64) / 3));
            int gap = Math.Max(12, btnW / 8);
            int total = btnW * 3 + gap * 2;
            int y = viewport.Height / 2;
            int x = (viewport.Width - total) / 2;
            return new PromptLayout
            {
                Yes = new Rectangle(x, y, btnW, btnH),
                No = new Rectangle(x + btnW + gap, y, btnW, btnH),
                Cancel = new Rectangle(x + (btnW + gap) * 2, y, btnW, btnH),
                QuestionY = y - Math.Max(50, viewport.Height / 10),
            };
        }

        // диалог с двумя кнопками (подтверждение выхода): пара строго
        // по центру экрана; ширина кнопок привязана к высоте экрана,
        // как и остальные пропорции игры (в фулскрине кнопки крупнее)
        internal static PromptLayout ComputeTwoButtonLayout(Viewport viewport)
        {
            int btnH = Math.Max(44, viewport.Height / 14);
            int gap = Math.Max(16, viewport.Height / 40);
            int btnW = Math.Clamp((int)(viewport.Height * 0.36f), 140, 340);
            btnW = Math.Min(btnW, (viewport.Width - 64 - gap) / 2);
            int total = btnW * 2 + gap;
            int y = viewport.Height / 2;
            int x = (viewport.Width - total) / 2;
            return new PromptLayout
            {
                Yes = new Rectangle(x, y, btnW, btnH),
                No = new Rectangle(x + btnW + gap, y, btnW, btnH),
                QuestionY = y - Math.Max(50, viewport.Height / 10),
            };
        }

        // затемнение + вопрос + кнопки; cancelText == null - диалог
        // из двух кнопок (раскладка тогда от ComputeTwoButtonLayout)
        internal static void DrawDialog(SpriteBatch sb, Texture2D pixel, FontSet fonts,
            Viewport viewport, string question, PromptLayout p,
            string yesText, Color yesBack, string noText, Color noBack,
            string cancelText = null, Color cancelBack = default)
        {
            sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), UiTheme.OverlayDim);
            DrawCentered(sb, fonts, question, viewport, p.QuestionY, 1.2f, UiTheme.Accent);
            DrawMenuButton(sb, pixel, fonts, p.Yes, yesText, yesBack);
            DrawMenuButton(sb, pixel, fonts, p.No, noText, noBack);
            if (cancelText != null)
                DrawMenuButton(sb, pixel, fonts, p.Cancel, cancelText, cancelBack);
        }

        internal static void DrawMenuButton(SpriteBatch sb, Texture2D pixel, FontSet fonts,
            Rectangle rect, string text, Color back)
        {
            sb.Draw(pixel, rect, back);
            SpriteFont sf = fonts.Pick(rect.Height * 0.45f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, rect.Width * 0.9f / sf.MeasureString(text).X);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text, new Vector2(rect.X + (rect.Width - size.X) / 2f,
                rect.Y + (rect.Height - size.Y) / 2f), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        internal static void DrawCentered(SpriteBatch sb, FontSet fonts, string text,
            Viewport viewport, int y, float baseScale, Color color)
        {
            SpriteFont sf = fonts.Pick(baseScale, out float scale);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text, new Vector2((viewport.Width - size.X) / 2f, y), color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
