using System;
using Lines.Shared.Input;
using Microsoft.JSInterop;

namespace Lines.Web
{
    // Ввод имени через браузерный диалог prompt() (синхронный
    // JS-интероп). Отмена диалога (null) - как кнопка «Пропустить»,
    // семантика та же, что у нативного диалога на Android.
    public class JsNamePrompter : INamePrompter
    {
        private readonly IJSInProcessRuntime js;

        public JsNamePrompter(IJSInProcessRuntime js)
        {
            this.js = js;
        }

        public void PromptName(string title, string message, string initial,
            Action<string> callback)
        {
            string name;
            try
            {
                name = js.Invoke<string>("prompt", title + "\n" + message, initial);
            }
            catch
            {
                name = null;
            }
            callback(name);
        }
    }
}
