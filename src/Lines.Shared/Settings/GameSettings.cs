namespace Lines.Shared.Settings
{
    // Модель настроек игры. Сериализуется в JSON (settings.json),
    // значения по умолчанию заданы инициализаторами - пропущенные
    // в файле поля остаются со значениями по умолчанию.
    public class GameSettings
    {
        // версия формата файла настроек (для будущих миграций)
        public int Version { get; set; } = 1;

        // звук: вкл/выкл
        public bool SoundEnabled { get; set; } = true;

        // громкость, 0-100%
        public int Volume { get; set; } = 80;

        // следы шариков (показ пути при перемещении)
        public bool ShowBallTrails { get; set; } = true;

        // режим экрана: true - полный экран, false - окно
        public bool Fullscreen { get; set; } = false;

        // имя игрока (спрашивается при первом запуске, предзаполняет
        // поле ввода имени при записи рекорда); пусто - ещё не спрашивали
        public string PlayerName { get; set; } = "";

        // автосохранение партии при выходе/сворачивании (слот 0)
        public bool AutoSaveEnabled { get; set; } = true;

        // язык интерфейса: "auto" (по локали системы/браузера),
        // "ru" или "en"
        public string Language { get; set; } = "auto";
    }
}
