using System;
using System.IO;

namespace Lines.Shared.Settings
{
    // Абстракция постоянного хранилища текстов (JSON) по логическому
    // имени ("settings", "saves", "results"). Windows/Android: файлы
    // в папке пользователя (FileKeyValueStore); Web (Blazor WASM):
    // localStorage браузера - реализация в хосте Lines.Web.
    public interface IKeyValueStore
    {
        // true, если значение найдено и прочитано
        bool TryRead(string name, out string text);
        void Write(string name, string text);
    }

    // Файловое хранилище: %AppData%/Lines/<name>.json на Windows,
    // внутренняя директория приложения на Android. Только Path.Combine.
    public class FileKeyValueStore : IKeyValueStore
    {
        public static string FilePath(string name) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Lines", name + ".json");

        public bool TryRead(string name, out string text)
        {
            text = null;
            string path = FilePath(name);
            if (TryReadFile(path, out text))
                return true;
            // основной файл битый (авария посреди старой записи) -
            // откатываемся на резервную копию предыдущей версии
            return TryReadFile(path + ".bak", out text);
        }

        private static bool TryReadFile(string path, out string text)
        {
            text = null;
            try
            {
                if (File.Exists(path))
                {
                    text = File.ReadAllText(path);
                    return true;
                }
            }
            catch
            {
                // битый/недоступный файл - читаем как «нет значения»
            }
            return false;
        }

        // Атомарная запись: пишем во временный файл и переименовываем
        // поверх основного (на NTFS/ext4 - атомарно); прежняя версия
        // сохраняется в .bak как страховка от аварии посреди записи.
        public void Write(string name, string text)
        {
            try
            {
                string path = FilePath(name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, text);
                if (File.Exists(path))
                    File.Copy(path, path + ".bak", overwrite: true);
                File.Move(tmp, path, overwrite: true);
            }
            catch
            {
                // запись недоступна (права, носитель) - работаем с данными в памяти
            }
        }
    }
}
