using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Переключение между экранами игры. Экран сам сигналит о переходе
    // (событие/колбэк), менеджер только держит текущий и проксирует
    // Update/Draw из LinesGame.
    public class ScreenManager
    {
        public IScreen Current { get; private set; }

        public void Set(IScreen screen)
        {
            Current = screen;
            // указатель мог остаться зажатым на предыдущем экране -
            // без сброса отпускание кнопки даст фантомный клик здесь
            Current?.ResetInput();
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            Current?.Update(gameTime, viewport);
        }

        // SpriteBatch.Begin/End - ответственность менеджера: экраны рисуют
        // только между ними (единый стек вызовов, без вложенных Begin).
        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (Current == null)
                return;
            spriteBatch.Begin();
            Current.Draw(spriteBatch, viewport);
            spriteBatch.End();
        }
    }
}
