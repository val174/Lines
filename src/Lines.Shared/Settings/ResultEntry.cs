namespace Lines.Shared.Settings
{
    // Одна запись таблицы рекордов.
    public class ResultEntry
    {
        // версия формата записи (для будущих миграций)
        public int Version { get; set; } = 1;

        public string Name { get; set; } = "Player";
        public int Score { get; set; }

        // дата и время игры, "dd.MM.yy HH:mm"; пусто у старых записей
        public string Date { get; set; } = "";
    }
}
