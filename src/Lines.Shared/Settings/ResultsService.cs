using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lines.Shared.Settings
{
    // Таблица рекордов: топ-10 по убыванию счёта, JSON через хранилище
    // (по умолчанию файл в папке пользователя, на Web - localStorage).
    // Порт логики старой result.json: счёт 0 не сохраняется, пустое
    // имя не сохраняется; если имя уже есть - остаётся лучший результат.
    public class ResultsService
    {
        public const int MaxEntries = 10;
        private const string StoreKey = "results";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private readonly IKeyValueStore store;

        public List<ResultEntry> Entries { get; } = new List<ResultEntry>();

        // срабатывает после изменения таблицы (Add/Clear) - подписчики
        // (GameScreen) инвалидируют кэш отформатированных строк
        public event Action Changed;

        public ResultsService(IKeyValueStore store = null)
        {
            this.store = store ?? new FileKeyValueStore();
            Load();
        }

        private void Load()
        {
            try
            {
                if (store.TryRead(StoreKey, out string json))
                {
                    var loaded = JsonSerializer.Deserialize<List<ResultEntry>>(json);
                    if (loaded != null)
                        Entries.AddRange(loaded.Where(e => !string.IsNullOrWhiteSpace(e.Name)));
                    SortAndTrim();
                }
            }
            catch
            {
                // битый JSON - начинаем с пустой таблицы
            }
        }

        // попадает ли счёт в таблицу (правила старой версии: ненулевой
        // и выше минимального, пока таблица неполная - любой ненулевой)
        public bool Qualifies(int score)
        {
            if (score <= 0)
                return false;
            if (Entries.Count < MaxEntries)
                return true;
            return score > Entries[Entries.Count - 1].Score;
        }

        // добавление результата; возвращает индекс новой записи
        // (для подсветки) или -1, если не попала в топ. Одинаковые
        // имена допускаются: в топе может быть несколько результатов
        // одного игрока
        public int Add(string name, int score)
        {
            if (string.IsNullOrWhiteSpace(name) || score <= 0)
                return -1;

            var entry = new ResultEntry
            {
                Name = name.Trim(),
                Score = score,
                Date = DateTime.Now.ToString("dd.MM.yy HH:mm")
            };
            Entries.Add(entry);

            SortAndTrim();
            Save();
            Changed?.Invoke();
            return Entries.IndexOf(entry);
        }

        // очистка таблицы (кнопка в настройках, с подтверждением)
        public void Clear()
        {
            Entries.Clear();
            Save();
            Changed?.Invoke();
        }

        private void SortAndTrim()
        {
            Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (Entries.Count > MaxEntries)
                Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);
        }

        private void Save()
        {
            store.Write(StoreKey, JsonSerializer.Serialize(Entries, JsonOptions));
        }
    }
}
