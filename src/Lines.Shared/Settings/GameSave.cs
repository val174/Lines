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

        // отмена хода: снимок состояния перед последним завершённым
        // ходом и счётчик отмен. Поля опциональны: у старых сохранений
        // их нет (null/0) - отмена в такой партии недоступна до первого
        // нового хода
        public int UndoCount { get; set; }
        public int[] UndoBalls { get; set; }     // построчно, как Balls; null - снимка нет
        public int[] UndoNextBalls { get; set; } // тройки x,y,color; null - снимка нет
        public int UndoScore { get; set; }       // счёт в снимке (до хода и до штрафа)
        public bool UndoUsed { get; set; }       // снимок уже отменён (до нового хода)
    }
}
