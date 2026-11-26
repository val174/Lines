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
        // («обновить сохранение?») и диалог новой игры.
        // В портретной ориентации кнопки - вертикальным столбцом
        // (в ряд на узком экране телефона они слишком узкие),
        // в ландшафте - в ряд; размеры масштабируются на UiTheme.Scale
        internal static PromptLayout ComputeThreeButtonLayout(Viewport viewport)
        {
            float s = UiTheme.Scale(viewport);

            if (viewport.Width < viewport.Height) // портрет
            {
                int btnW = Math.Min((int)(viewport.Width * 0.86f), (int)(560 * s));
                int btnH = Math.Max((int)(44 * s), viewport.Height / 14);
                int vgap = Math.Max((int)(12 * s), btnH / 3);
                int totalH = btnH * 3 + vgap * 2;
                int x = (viewport.Width - btnW) / 2;
                int y = (viewport.Height - totalH) / 2;
                return new PromptLayout
                {
                    Yes = new Rectangle(x, y, btnW, btnH),
                    No = new Rectangle(x, y + btnH + vgap, btnW, btnH),
                    Cancel = new Rectangle(x, y + (btnH + vgap) * 2, btnW, btnH),
                    QuestionY = y - Math.Max((int)(80 * s), viewport.Height / 6),
                };
            }

            // ландшафт: кнопки в ряд
            int btnHl = Math.Max((int)(44 * s), viewport.Height / 14);
            int btnWl = Math.Max((int)(110 * s), Math.Min((int)(200 * s), (viewport.Width - 64) / 3));
            int gap = Math.Max((int)(12 * s), btnWl / 8);
            int total = btnWl * 3 + gap * 2;
            int yl = viewport.Height / 2;
            int xl = (viewport.Width - total) / 2;
            return new PromptLayout
            {
                Yes = new Rectangle(xl, yl, btnWl, btnHl),
                No = new Rectangle(xl + btnWl + gap, yl, btnWl, btnHl),
                Cancel = new Rectangle(xl + (btnWl + gap) * 2, yl, btnWl, btnHl),
                QuestionY = yl - Math.Max((int)(80 * s), viewport.Height / 6),
            };
        }

        // диалог с двумя кнопками (подтверждение выхода): пара строго
        // по центру экрана; ширина кнопок привязана к высоте экрана,
        // как и остальные пропорции игры (в фулскрине кнопки крупнее);
        // потолки размеров масштабируются на UiTheme.Scale
        internal static PromptLayout ComputeTwoButtonLayout(Viewport viewport)
        {
            float s = UiTheme.Scale(viewport);
            int btnH = Math.Max((int)(44 * s), viewport.Height / 14);
            int gap = Math.Max((int)(16 * s), viewport.Height / 40);
            int btnW = Math.Clamp((int)(viewport.Height * 0.36f), (int)(140 * s), (int)(340 * s));
            btnW = Math.Min(btnW, (viewport.Width - 64 - gap) / 2);
            int total = btnW * 2 + gap;
            int y = viewport.Height / 2;
            int x = (viewport.Width - total) / 2;
            return new PromptLayout
            {
                Yes = new Rectangle(x, y, btnW, btnH),
                No = new Rectangle(x + btnW + gap, y, btnW, btnH),
                QuestionY = y - Math.Max((int)(80 * s), viewport.Height / 6),
            };
        }

        // затемнение + вопрос + кнопки; cancelText == null - диалог
        // из двух кнопок (раскладка тогда от ComputeTwoButtonLayout).
        // questionBaseScale - масштаб надписи вопроса (умножается на
        // TextScale; по умолчанию обычный, для важных диалогов - крупнее)
        internal static void DrawDialog(SpriteBatch sb, Texture2D pixel, FontSet fonts,
            Viewport viewport, string question, PromptLayout p,
            string yesText, Color yesBack, string noText, Color noBack,
            string cancelText = null, Color cancelBack = default,
            float questionBaseScale = 1.2f)
        {
            sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), UiTheme.OverlayDim);
            DrawCentered(sb, fonts, question, viewport, p.QuestionY,
                questionBaseScale * UiTheme.TextScale(viewport), UiTheme.Accent);
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
            // защита от вылезания за края на узких экранах
            scale = Math.Min(scale, viewport.Width * 0.92f / Math.Max(1f, sf.MeasureString(text).X));
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text, new Vector2((viewport.Width - size.X) / 2f, y), color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
