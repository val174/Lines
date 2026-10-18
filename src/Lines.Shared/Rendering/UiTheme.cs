using Microsoft.Xna.Framework;

namespace Lines.Shared.Rendering
{
    // Централизованная палитра UI и общие коэффициенты отрисовки.
    // Значения собраны из экранов как есть - только те, что встречаются
    // 2+ раза (одноразовые эмпирические подборки остаются на месте).
    // ВАЖНО: значения не менять - внешний вид должен остаться прежним.
    public static class UiTheme
    {
        public static readonly Color Panel = new Color(48, 46, 40);      // фон панелей, ячеек, плашек
        public static readonly Color PanelAlt = new Color(60, 57, 48);   // неактивные кнопки/вкладки, слоты
        public static readonly Color BoardFrame = new Color(96, 88, 64); // рамка поля, бордюры ячеек
        public static readonly Color CellBack = new Color(189, 178, 127); // фон клетки поля («песочный»)
        public static readonly Color Accent = new Color(240, 205, 80);   // золотые заголовки и акценты
        public static readonly Color Confirm = new Color(52, 110, 60);   // зелёная кнопка («Да», «Сохранить», вкл)
        public static readonly Color Danger = new Color(110, 56, 52);    // красная кнопка («Нет», «Отмена», выкл)
        public static readonly Color Neutral = new Color(80, 76, 64);    // нейтральная кнопка («Назад», «Пропустить»)
        public static readonly Color OverlayDim = new Color(0, 0, 0, 170); // затемнение под диалогами
        public static readonly Color FooterText = new Color(180, 180, 180); // подсказки и футеры

        // размер шарика относительно клетки (и стоячий, и летящий)
        public const float BallScale = 0.92f;
    }
}
