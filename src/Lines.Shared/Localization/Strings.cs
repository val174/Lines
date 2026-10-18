using System.Globalization;

namespace Lines.Shared.Localization
{
    // Локализация UI (русский/английский). Все пользовательские строки
    // - свойства Strings; активный язык - Strings.Current, задаётся из
    // LinesGame.ApplySettings по настройке GameSettings.Language
    // ("auto" - по локали системы/браузера). «Шарики» - бренд и
    // не переводится. Шрифты менять не нужно: латиница (ASCII 32-126)
    // и кириллица (1024-1279) уже есть во всех SpriteFont.
    public class Strings
    {
        public static Strings Current { get; set; } = Ru;

        public static readonly Strings Ru = new StringsRu();
        public static readonly Strings En = new Strings();

        // выбор языка: "ru"/"en" - явно, "auto"/пусто - по локали
        // системы (Web: локаль браузера); русская локаль -> Ru,
        // всё остальное -> En (fallback)
        public static Strings Resolve(string language)
        {
            string lang = string.IsNullOrEmpty(language) ? "auto" : language;
            if (lang == "auto")
                lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return lang == "ru" ? Ru : En;
        }

        // --- английский (базовый класс = fallback-язык) ---

        // верхняя панель и пьедесталы
        public virtual string ScoreLabel => "Score: ";
        public virtual string Leader => "Leader";
        public virtual string Contender => "Contender";

        // нижняя панель и общие кнопки
        public virtual string NewGame => "New game";
        public virtual string Save => "Save";
        public virtual string Load => "Load";
        public virtual string Settings => "Settings";
        public virtual string Records => "Records";
        public virtual string Exit => "Exit";
        public virtual string Back => "Back";
        public virtual string Yes => "Yes";
        public virtual string No => "No";
        public virtual string Cancel => "Cancel";
        public virtual string Skip => "Skip";
        public virtual string On => "On";
        public virtual string Off => "Off";

        // настройки
        public virtual string Sound => "Sound";
        public virtual string BallTrails => "Ball trails";
        public virtual string AutoSave => "Autosave";
        public virtual string WindowMode => "Window";
        public virtual string FullscreenMode => "Fullscreen";
        public virtual string Clear => "Clear";
        public virtual string ConfirmPrompt => "Confirm?";
        public virtual string VolumeFormat => "Volume: {0}%";
        public virtual string Language => "Language";
        public virtual string LanguageAuto => "Auto";

        // конец игры и рекорды
        public virtual string GameOver => "Game over";
        public virtual string NewRecordEnterName => "New record! Enter your name:";
        public virtual string NewRecordTitle => "New record!";
        public virtual string EnterNamePrompt => "Enter your name:";
        public virtual string ClickForNewGame => "Click to start a new game";
        public virtual string ClickToReturn => "Click to return";

        // первый запуск
        public virtual string Welcome => "Welcome!";
        public virtual string AskName => "What's your name?";

        // диалоги поверх поля
        public virtual string UpdateSavePrompt => "Update your save?";
        public virtual string NewGamePrompt => "Start a new game?";
        public virtual string ExitConfirmPrompt => "Exit the game?";

        // экран сохранённых игр
        public virtual string SavedGames => "Saved games";
        public virtual string EmptySlot => "Empty";
        public virtual string OverwritePrompt => "Overwrite?";
        public virtual string AutoWatermark => "AUTO";
        public virtual string SaveHintBusy => "Cannot save now: move in progress or game over";
        public virtual string SaveHintChoose => "Choose a slot to save";
        public virtual string SaveCellFormat => "Score {0} - {1}";

        // заставка
        public virtual string VersionAuthorFormat => "Version {0}   Author: {1}";

        // относительное время (RelativeTime)
        public virtual string JustNow => "just now";
        public virtual string MinutesAgo(int n) => Ago(n, "minute", "minutes");
        public virtual string HoursAgo(int n) => Ago(n, "hour", "hours");
        public virtual string DaysAgo(int n) => Ago(n, "day", "days");
        public virtual string MonthsAgo(int n) => Ago(n, "month", "months");
        public virtual string YearsAgo(int n) => Ago(n, "year", "years");

        private static string Ago(int n, string one, string many) =>
            $"{n} {(n == 1 ? one : many)} ago";
    }

    // русский: плюрализация с тремя формами (1 минута, 2 минуты, 5 минут)
    public class StringsRu : Strings
    {
        public override string ScoreLabel => "Счёт: ";
        public override string Leader => "Лидер";
        public override string Contender => "Претендент";

        public override string NewGame => "Новая игра";
        public override string Save => "Сохранить";
        public override string Load => "Загрузить";
        public override string Settings => "Настройки";
        public override string Records => "Рекорды";
        public override string Exit => "Выход";
        public override string Back => "Назад";
        public override string Yes => "Да";
        public override string No => "Нет";
        public override string Cancel => "Отмена";
        public override string Skip => "Пропустить";
        public override string On => "Вкл";
        public override string Off => "Выкл";

        public override string Sound => "Звук";
        public override string BallTrails => "Следы шариков";
        public override string AutoSave => "Автосохранение";
        public override string WindowMode => "Окно";
        public override string FullscreenMode => "Полный экран";
        public override string Clear => "Очистить";
        public override string ConfirmPrompt => "Подтвердить?";
        public override string VolumeFormat => "Громкость: {0}%";
        public override string Language => "Язык";
        public override string LanguageAuto => "Авто";

        public override string GameOver => "Игра окончена";
        public override string NewRecordEnterName => "Новый рекорд! Введите имя:";
        public override string NewRecordTitle => "Новый рекорд!";
        public override string EnterNamePrompt => "Введите имя:";
        public override string ClickForNewGame => "Нажмите для новой игры";
        public override string ClickToReturn => "Нажмите для возврата";

        public override string Welcome => "Добро пожаловать!";
        public override string AskName => "Как вас зовут?";

        public override string UpdateSavePrompt => "Обновить ваше сохранение?";
        public override string NewGamePrompt => "Начать новую игру?";
        public override string ExitConfirmPrompt => "Выйти из игры?";

        public override string SavedGames => "Сохранённые игры";
        public override string EmptySlot => "Пусто";
        public override string OverwritePrompt => "Перезаписать?";
        public override string AutoWatermark => "АВТО";
        public override string SaveHintBusy => "Сейчас сохранить нельзя: идёт ход или игра окончена";
        public override string SaveHintChoose => "Выберите ячейку для сохранения";
        public override string SaveCellFormat => "Счёт {0} - {1}";

        public override string VersionAuthorFormat => "Версия {0}   Автор: {1}";

        public override string JustNow => "только что";
        public override string MinutesAgo(int n) => AgoRu(n, "минута", "минуты", "минут");
        public override string HoursAgo(int n) => AgoRu(n, "час", "часа", "часов");
        public override string DaysAgo(int n) => AgoRu(n, "день", "дня", "дней");
        public override string MonthsAgo(int n) => AgoRu(n, "месяц", "месяца", "месяцев");
        public override string YearsAgo(int n) => AgoRu(n, "год", "года", "лет");

        private static string AgoRu(int n, string one, string few, string many) =>
            $"{n} {Plural(n, one, few, many)} назад";

        private static string Plural(int n, string one, string few, string many)
        {
            n = System.Math.Abs(n) % 100;
            if (n >= 11 && n <= 14)
                return many;
            switch (n % 10)
            {
                case 1: return one;
                case 2:
                case 3:
                case 4: return few;
                default: return many;
            }
        }
    }
}
