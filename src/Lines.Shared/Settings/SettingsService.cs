using System;
using System.Text.Json;

namespace Lines.Shared.Settings
{
    // Загрузка/сохранение настроек в JSON через хранилище (по умолчанию
    // файл в папке пользователя, на Web - localStorage; см. IKeyValueStore).
    public class SettingsService
    {
        private const string StoreKey = "settings";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private readonly IKeyValueStore store;

        public GameSettings Current { get; }

        // срабатывает после любого изменения настроек - подписчики
        // (LinesGame, GameScreen) применяют их к себе
        public event Action Changed;

        public SettingsService(IKeyValueStore store = null)
        {
            this.store = store ?? new FileKeyValueStore();
            Current = Load();
        }

        private GameSettings Load()
        {
            if (store.TryRead(StoreKey, out string json))
            {
                try
                {
                    return JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
                }
                catch
                {
                    // битый JSON - тихо откатываемся на значения по умолчанию
                }
            }
            return new GameSettings();
        }

        // изменение настроек через мутацию + немедленное сохранение и уведомление
        public void Update(Action<GameSettings> mutate)
        {
            mutate(Current);
            Current.Volume = Math.Clamp(Current.Volume, 0, 100);
            Save();
            Changed?.Invoke();
        }

        private void Save()
        {
            store.Write(StoreKey, JsonSerializer.Serialize(Current, JsonOptions));
        }
    }
}
