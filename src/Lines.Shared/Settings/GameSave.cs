namespace Lines.Shared.Settings
{
    // Сохранённая партия: расположение шариков (построчно, 0 - пусто,
    // -1 - резервация под подсказку следующего шарика, 1..6 - цвет),
    // следующие шарики (тройки x,y,color) и счёт
    public class GameSave
    {
        // версия формата сохранения (для будущих миграций)
        public int Version { get; set; } = 1;

        public int MapSize { get; set; } = 9;
        public int[] Balls { get; set; } = new int[81];
        public int[] NextBalls { get; set; } = new int[9]; // x,y,color * 3
        public int Score { get; set; }
        public string Date { get; set; } = "";
    }
}
