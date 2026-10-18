using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Lines.Shared.Input
{
    // Единый ввод указателем: мышь (Windows) и тап (Android).
    // Click - экранные координаты клика/тапа текущего кадра (null, если не было);
    // IsDown/Position - указатель удерживается (для ползунков и перетаскивания);
    // перевод в координаты клетки - через BoardLayout.TryGetCell.
    public class PointerInput
    {
        private MouseState previousMouse;
        // глотаем отпускание, если кнопка/палец были зажаты уже до
        // смены экрана (Reset) - иначе оно дало бы фантомный клик
        // на новом экране
        private bool swallowMouse;
        private bool swallowTouches;
        // тач: клик засчитывается, только если нажатие пальца видели мы
        private bool touchSeen;

        public Point? Click { get; private set; }

        public bool IsDown { get; private set; }
        public Point Position { get; private set; }

        // сброс при программной смене экрана (ScreenManager.Set):
        // зажатые сейчас кнопка/палец «проглатываются» до отпускания
        public void Reset()
        {
            Click = null;
            IsDown = false;
            MouseState mouse = Mouse.GetState();
            swallowMouse = mouse.LeftButton == ButtonState.Pressed;
            previousMouse = mouse;
            swallowTouches = TouchPanel.GetState().Count > 0;
            touchSeen = false;
        }

        public void Update()
        {
            Click = null;

            // клик мыши - по отпусканию кнопки
            MouseState mouse = Mouse.GetState();
            if (mouse.LeftButton == ButtonState.Released)
            {
                if (previousMouse.LeftButton == ButtonState.Pressed && !swallowMouse)
                    Click = new Point(mouse.X, mouse.Y);
                swallowMouse = false;
            }
            IsDown = mouse.LeftButton == ButtonState.Pressed;
            Position = new Point(mouse.X, mouse.Y);
            previousMouse = mouse;

            // тап - по отпусканию пальца; удержание - пока палец на экране
            TouchCollection touches = TouchPanel.GetState();
            if (touches.Count == 0)
                swallowTouches = false;
            bool touchActive = false;
            foreach (TouchLocation touch in touches)
            {
                if (touch.State == TouchLocationState.Released)
                {
                    if (touchSeen && !swallowTouches)
                        Click = new Point((int)touch.Position.X, (int)touch.Position.Y);
                }
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    touchActive = true;
                    IsDown = true;
                    Position = new Point((int)touch.Position.X, (int)touch.Position.Y);
                }
            }
            touchSeen = touchActive && !swallowTouches;
        }
    }
}
