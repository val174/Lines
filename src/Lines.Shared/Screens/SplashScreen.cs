using System;
using System.Reflection;
using Lines.Shared.Input;
using Lines.Shared.Localization;
using Lines.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Экран заставки: надпись «Шарики» сверху, картинка игрового поля
    // (Content/splash.png, генерируется build/generate-splash.py) с красным
    // шариком в короне в центре, строка состояния снизу (версия + автор).
    // Переход в игру - по клику/тапу или автоматически через
    // SplashDurationSeconds (что наступит раньше).
    public class SplashScreen : IScreen
    {
        // длительность показа заставки, сек; скорректировать после первых тестов
        public const float SplashDurationSeconds = 10f;

        // автор - константа из спецификации
        public const string Author = "Val + Kimi";

        private readonly FontSet fonts;
        private readonly Texture2D splash;
        private readonly Texture2D pixel;
        private readonly PointerInput pointer = new PointerInput();
        private readonly string version;
        private float elapsed;

        // срабатывает один раз - по клику/тапу или по таймауту
        public event Action Finished;

        public SplashScreen(GraphicsDevice device, ContentManager content)
        {
            fonts = new FontSet(content);
            splash = content.Load<Texture2D>("splash");
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            version = GetVersion();
        }

        // ScreenManager вызывает при переключении на этот экран
        public void ResetInput() => pointer.Reset();

        public void Update(GameTime gameTime, Viewport viewport)
        {
            elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            pointer.Update();
            if (pointer.Click.HasValue || elapsed >= SplashDurationSeconds)
                Finished?.Invoke();
        }

        public void Draw(SpriteBatch sb, Viewport viewport)
        {
            DrawTitle(sb, viewport);
            DrawPicture(sb, viewport);
            DrawStatusLine(sb, viewport);
        }

        // надпись «Шарики» крупным шрифтом в верхней части экрана
        private void DrawTitle(SpriteBatch sb, Viewport viewport)
        {
            const string title = "Шарики";
            int bandHeight = Math.Max(60, viewport.Height / 5);
            SpriteFont sf = fonts.Pick(bandHeight * 0.7f / fonts.Base.LineSpacing, out float scale);
            scale = Math.Min(scale, viewport.Width * 0.8f / sf.MeasureString(title).X);
            Vector2 size = sf.MeasureString(title) * scale;
            sb.DrawString(sf, title,
                new Vector2((viewport.Width - size.X) / 2f, (bandHeight - size.Y) / 2f),
                UiTheme.Accent, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // картинка: квадратный splash.png по центру средней части экрана
        private void DrawPicture(SpriteBatch sb, Viewport viewport)
        {
            int top = Math.Max(60, viewport.Height / 5);
            int bottom = Math.Max(28, viewport.Height / 14); // полоса строки состояния
            int availableH = viewport.Height - top - bottom;

            int side = Math.Min((int)(viewport.Width * 0.86f), (int)(availableH * 0.92f));
            var dest = new Rectangle((viewport.Width - side) / 2, top + (availableH - side) / 2, side, side);
            sb.Draw(splash, dest, Color.White);
        }

        // строка состояния внизу: версия игры и автор
        private void DrawStatusLine(SpriteBatch sb, Viewport viewport)
        {
            int bandHeight = Math.Max(28, viewport.Height / 14);
            sb.Draw(pixel, new Rectangle(0, viewport.Height - bandHeight, viewport.Width, bandHeight),
                new Color(30, 29, 26));

            string text = string.Format(Strings.Current.VersionAuthorFormat, version, Author);
            SpriteFont sf = fonts.Pick(Math.Min(1f, bandHeight * 0.55f / fonts.Base.LineSpacing),
                out float scale);
            Vector2 size = sf.MeasureString(text) * scale;
            sb.DrawString(sf, text,
                new Vector2((viewport.Width - size.X) / 2f, viewport.Height - bandHeight + (bandHeight - size.Y) / 2f),
                new Color(200, 195, 180), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // версия игры из AssemblyInformationalVersion (без суффикса +hash)
        private static string GetVersion()
        {
            Assembly asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            string v = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?? asm.GetName().Version?.ToString()
                       ?? "?";
            int plus = v.IndexOf('+');
            return plus >= 0 ? v.Substring(0, plus) : v;
        }
    }
}
