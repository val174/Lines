using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Screens
{
    // Единый контракт экрана игры (заставка, поле, настройки).
    // Экран получает Viewport каждый кадр и сам считает раскладку -
    // никаких фиксированных размеров.
    public interface IScreen
    {
        void Update(GameTime gameTime, Viewport viewport);
        void Draw(SpriteBatch spriteBatch, Viewport viewport);

        // сброс состояния указателя при программной смене экрана:
        // зажатая на предыдущем экране кнопка/палец не должны дать
        // фантомный клик на новом (вызывается из ScreenManager.Set)
        void ResetInput();
    }
}
