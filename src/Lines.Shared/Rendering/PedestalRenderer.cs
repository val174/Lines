using System;
using Lines.Shared.Localization;
using Lines.Shared.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Rendering
{
    // Пьедесталы лидера и претендента по бокам поля (только ландшафт):
    // колонна с персонажем в короне и подписью. Высота претендента
    // растёт со счётом до рекорда лидера; лидер стоит на максимуме,
    // пока его не обогнали, затем уменьшается.
    internal class PedestalRenderer
    {
        private readonly FontSet fonts;
        private readonly BallRenderer balls;
        private readonly Texture2D pixel;
        private readonly Texture2D kingLeader; // персонаж лидера (Content/king_leader.png)
        private readonly Texture2D kingPlayer; // персонаж претендента (Content/king_player.png)
        private readonly ResultsService results;

        public PedestalRenderer(ContentManager content, FontSet fonts, BallRenderer balls,
            Texture2D pixel, ResultsService results)
        {
            this.fonts = fonts;
            this.balls = balls;
            this.pixel = pixel;
            this.results = results;
            kingLeader = content.Load<Texture2D>("king_leader");
            kingPlayer = content.Load<Texture2D>("king_player");
        }

        public void Draw(SpriteBatch sb, BoardLayout layout, Viewport viewport, int score)
        {
            if (layout.IsPortrait)
                return; // пьедесталы - только в горизонтальной ориентации
            int marginL = layout.BoardOriginX;
            int marginR = viewport.Width - (layout.BoardOriginX + layout.BoardSize);
            int margin = Math.Min(marginL, marginR);
            if (margin < 70)
                return; // узкие поля - пьедесталам нет места

            // лидер из таблицы рекордов; пустая таблица - условный «Лидер» 100
            string leaderName = Strings.Current.Leader;
            int leaderScore = 100;
            if (results.Entries.Count > 0)
            {
                leaderName = results.Entries[0].Name;
                leaderScore = results.Entries[0].Score;
            }

            // рост претендента пропорционален счёту до рекорда лидера;
            // лидер стоит на максимуме, пока его не обогнали, затем
            // уменьшается пропорционально превышению рекорда
            float tContender = leaderScore > 0
                ? MathHelper.Clamp(score / (float)leaderScore, 0f, 1f)
                : 1f;
            float tLeader = score > leaderScore
                ? MathHelper.Clamp(leaderScore / (float)score, 0f, 1f)
                : 1f;

            DrawPedestal(sb, layout, viewport, marginL / 2, margin, tLeader,
                kingLeader, leaderName, leaderScore);
            DrawPedestal(sb, layout, viewport, viewport.Width - marginR / 2, margin, tContender,
                kingPlayer, Strings.Current.Contender, score);
        }

        private void DrawPedestal(SpriteBatch sb, BoardLayout layout, Viewport viewport,
            int centerX, int margin, float frac, Texture2D character,
            string name, int points)
        {
            // ширина персонажа под ширину поля; колонна - пропорционально ей
            int charW = MathHelper.Clamp((int)(margin * 0.85f), 40, 150);
            int charH = charW * character.Height / character.Width;
            int colW = (int)(charW * 0.6f);

            SpriteFont sf = fonts.Pick(0.62f, out float scale);
            // подгоняем масштаб подписи под ширину поля
            scale = Math.Min(scale, (margin - 8) / Math.Max(1f, sf.MeasureString(name).X));
            int lineH = (int)(sf.LineSpacing * scale);
            int labelBand = lineH * 2 + 14;

            int baseY = layout.StatusBarRect.Top - labelBand - 4; // низ колонны - над нижней панелью
            // запас сверху под персонажа, чтобы не залезал на панель
            int maxColH = baseY - layout.PanelRect.Bottom - charH - 8;
            if (maxColH < 20)
                return;
            int colH = (int)(maxColH * (0.12f + 0.88f * frac));

            // колонна: база из двух плит, ствол с каннелюрами, капитель
            Color stone = new Color(186, 176, 150);
            Color stoneDark = new Color(158, 148, 124);
            Color stoneLight = new Color(208, 199, 172);

            // высоту базы и капители ограничиваем пропорцией к ширине,
            // иначе на высокой колонне плиты растягиваются
            int baseH = Math.Clamp(colH / 9, 6, (int)(colW * 0.6f));
            int capH = Math.Clamp(colH / 9, 6, (int)(colW * 0.6f));
            int shaftH = colH - baseH - capH;

            // база: две широкие плиты снизу
            int baseW1 = (int)(colW * 1.5f), baseW2 = (int)(colW * 1.3f);
            int bh1 = baseH / 2, bh2 = baseH - bh1;
            sb.Draw(pixel, new Rectangle(centerX - baseW1 / 2, baseY - bh1, baseW1, bh1), stoneDark);
            sb.Draw(pixel, new Rectangle(centerX - baseW2 / 2, baseY - baseH, baseW2, bh2), stone);

            // ствол с вертикальными каннелюрами (тёмные бороздки)
            int shaftY = baseY - baseH - shaftH;
            var shaft = new Rectangle(centerX - colW / 2, shaftY, colW, shaftH);
            sb.Draw(pixel, shaft, stone);
            int flutes = Math.Max(3, colW / 10);
            int fluteW = Math.Max(1, colW / (flutes * 2));
            for (int i = 0; i < flutes; i++)
            {
                int fx = shaft.X + (2 * i + 1) * shaft.Width / (flutes * 2) - fluteW / 2;
                sb.Draw(pixel, new Rectangle(fx, shaft.Y, fluteW, shaft.Height), stoneDark);
            }

            // капитель: две плиты, расширяющиеся кверху
            int capW1 = (int)(charW * 0.62f), capW2 = (int)(charW * 0.78f);
            int ch1 = capH / 2, ch2 = capH - ch1;
            sb.Draw(pixel, new Rectangle(centerX - capW1 / 2, shaftY - ch1, capW1, ch1), stone);
            sb.Draw(pixel, new Rectangle(centerX - capW2 / 2, shaftY - capH, capW2, ch2), stoneLight);
            int colTop = shaftY - capH;

            // персонаж стоит на площадке, тень под ним
            balls.DrawShadow(sb, new Rectangle(centerX - charW / 3, colTop - charW / 16,
                charW * 2 / 3, charW / 8), 0.5f);
            sb.Draw(character, new Rectangle(centerX - charW / 2, colTop - charH,
                charW, charH), Color.White);

            // подпись под пьедесталом: имя и очки
            Vector2 nameSize = sf.MeasureString(name) * scale;
            sb.DrawString(sf, name, new Vector2(centerX - nameSize.X / 2f, baseY + 6),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            string pts = points.ToString();
            Vector2 ptsSize = sf.MeasureString(pts) * scale;
            sb.DrawString(sf, pts, new Vector2(centerX - ptsSize.X / 2f, baseY + 6 + lineH),
                UiTheme.Accent, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
