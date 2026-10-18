using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Rendering
{
    // Набор одного шрифта в нескольких размерах (24/64/128). Нужен,
    // чтобы текст не растягивался из мелкой текстуры на больших экранах
    // (в фулскрине upscale SpriteFont даёт заметные пиксели): берётся
    // наименьший шрифт, который не придётся увеличивать.
    public class FontSet
    {
        private readonly SpriteFont[] fonts; // по возрастанию LineSpacing

        public FontSet(ContentManager content)
        {
            fonts = new[]
            {
                content.Load<SpriteFont>("MainFont"),
                content.Load<SpriteFont>("MainFont64"),
                content.Load<SpriteFont>("MainFont128"),
            };
        }

        // базовый (24px) шрифт - для раскладок, завязанных на LineSpacing
        public SpriteFont Base => fonts[0];

        // baseScale - желаемый масштаб относительно базового шрифта,
        // как считался раньше; возвращает шрифт и реальный scale под него
        public SpriteFont Pick(float baseScale, out float scale)
        {
            float target = fonts[0].LineSpacing * baseScale;
            foreach (SpriteFont f in fonts)
                if (f.LineSpacing >= target)
                {
                    scale = target / f.LineSpacing;
                    return f;
                }
            SpriteFont biggest = fonts[fonts.Length - 1];
            scale = target / biggest.LineSpacing;
            return biggest;
        }
    }
}
