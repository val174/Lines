using Lines.Shared.Settings;
using Microsoft.JSInterop;

namespace Lines.Web
{
    // Хранилище настроек/сейвов/рекордов в localStorage браузера.
    // Синхронный интероп (IJSInProcessRuntime) - код сервисов синхронный.
    public class LocalStorageStore : IKeyValueStore
    {
        private const string Prefix = "lines.";
        private readonly IJSInProcessRuntime js;

        public LocalStorageStore(IJSInProcessRuntime js)
        {
            this.js = js;
        }

        public bool TryRead(string name, out string text)
        {
            text = null;
            try
            {
                text = js.Invoke<string>("localStorage.getItem", Prefix + name);
            }
            catch
            {
                // localStorage недоступен (приватный режим и т.п.)
            }
            return text != null;
        }

        public void Write(string name, string text)
        {
            try
            {
                js.InvokeVoid("localStorage.setItem", Prefix + name, text);
            }
            catch
            {
                // переполнение квоты/запрет - игра работает с данными в памяти
            }
        }
    }
}
