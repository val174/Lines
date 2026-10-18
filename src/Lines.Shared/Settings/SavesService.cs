using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Lines.Shared.Settings
{
    // Сохранённые партии: 9 ручных слотов + 1 автосохранение (слот 0,
    // всегда перезаписывается). Хранение - JSON через хранилище
    // (по умолчанию файл в папке пользователя, на Web - localStorage).
    // Пустой слот - null.
    public class SavesService
    {
        public const int SlotCount = 10;
        public const int AutoSlot = 0;
        private const string StoreKey = "saves";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private readonly IKeyValueStore store;
        private readonly List<GameSave> slots = new List<GameSave>();

        public IReadOnlyList<GameSave> Slots => slots;

        public SavesService(IKeyValueStore store = null)
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
                    var loaded = JsonSerializer.Deserialize<List<GameSave>>(json);
                    if (loaded != null)
                        slots.AddRange(loaded);
                }
            }
            catch
            {
                // битый JSON - начинаем с пустых слотов
                slots.Clear();
            }

            // фиксированное число слотов, недостающие - пустые
            while (slots.Count < SlotCount)
                slots.Add(null);
            if (slots.Count > SlotCount)
                slots.RemoveRange(SlotCount, slots.Count - SlotCount);
        }

        // запись в слот (перезапись занятого - по решению пользователя)
        public void Save(int index, GameSave save)
        {
            if (index < 0 || index >= SlotCount)
                return;
            save.Date = DateTime.Now.ToString("dd.MM.yy HH:mm");
            slots[index] = save;
            Persist();
        }

        // автосохранение - всегда в один и тот же слот
        public void AutoSave(GameSave save) => Save(AutoSlot, save);

        private void Persist()
        {
            store.Write(StoreKey, JsonSerializer.Serialize(slots, JsonOptions));
        }
    }
}
