using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Rendering
{
    // Отрисовка шариков: текстуры генерируются заранее скриптом
    // build/generate-balls.py (512px, сферическое освещение, блик,
    // контровый свет, суперсэмплинг 4x) и грузятся из контента.
    // Тень по-прежнему рисуется программно - она одна на всех.
    public class BallRenderer
    {
        public const int ColorCount = 6;

        private readonly Texture2D[] balls = new Texture2D[ColorCount];
        private readonly Texture2D shadow;

        // палитра классических «Линий» (синхронизирована с PALETTE
        // в build/generate-balls.py): красный, жёлтый, синий,
        // зелёный, бирюзовый, фиолетовый
        public static readonly Color[] Palette =
        {
            new Color(215, 40, 40),
            new Color(225, 205, 40),
            new Color(60, 80, 220),
            new Color(40, 180, 80),
            new Color(50, 190, 200),
            new Color(190, 60, 200),
        };

        public BallRenderer(GraphicsDevice device, ContentManager content)
        {
            for (int i = 0; i < ColorCount; i++)
                balls[i] = content.Load<Texture2D>("Balls/ball_" + (i + 1));
            shadow = CreateShadowTexture(device, 128);
        }

        // colorIndex - 1..6, как в LogicLines
        public void Draw(SpriteBatch spriteBatch, int colorIndex, Rectangle dest,
            float alpha = 1f, float yOffset = 0f)
        {
            if (colorIndex < 1 || colorIndex > ColorCount)
                return;
            Rectangle r = dest;
            r.Y += (int)yOffset;
            spriteBatch.Draw(balls[colorIndex - 1], r, Color.White * alpha);
        }

        // белая копия поверх - «вспышка» при сжигании линии
        public void DrawTinted(SpriteBatch spriteBatch, int colorIndex, Rectangle dest, Color tint)
        {
            if (colorIndex < 1 || colorIndex > ColorCount)
                return;
            spriteBatch.Draw(balls[colorIndex - 1], dest, tint);
        }

        // тень рисуем отдельно, чтобы не «прыгала» вместе с шариком
        public void DrawShadow(SpriteBatch spriteBatch, Rectangle dest, float alpha = 1f)
        {
            Rectangle r = dest;
            r.Inflate(dest.Width / 10, 0);
            r.Y += dest.Height / 10;
            spriteBatch.Draw(shadow, r, Color.White * alpha);
        }

        private static Texture2D CreateShadowTexture(GraphicsDevice device, int size)
        {
            var data = new Color[size * size];
            float c = size / 2f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // мягкий эллипс с затуханием к краям
                    float nx = (x - c + 0.5f) / (size / 2f);
                    float ny = (y - c + 0.5f) / (size / 4f);
                    float d2 = nx * nx + ny * ny;
                    int a = d2 >= 1f ? 0 : (int)(120f * (1f - d2) * (1f - d2));
                    data[y * size + x] = new Color(0, 0, 0, a);
                }

            var texture = new Texture2D(device, size, size);
            texture.SetData(data);
            return texture;
        }
    }
}
