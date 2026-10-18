using System;
using Lines.Shared;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace Lines.Web.Pages
{
    public partial class Index
    {
        LinesGame _game;
        private static string _phase = "ctor";

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            try
            {
                // init game
                if (_game == null)
                {
                    var js = (IJSInProcessRuntime)JsRuntime;
                    _phase = "ctor";
                    _game = new LinesGame(new LocalStorageStore(js))
                    {
                        // ввод имени - JS-диалог prompt(); SDL TextInput на WebGL нет
                        NamePrompter = new JsNamePrompter(js),
                    };
                    _phase = "exit-sub";
                    // «выход» в браузере невозможен - возвращаемся на заставку
                    _game.ExitRequested += _game.ShowSplash;
                    _phase = "run";
                    _game.Run();
                    _phase = "run-ok";
                }

                // run gameloop
                _game.Tick();
            }
            catch (Exception ex)
            {
                // диагностика: текст managed-исключения - в DOM (консоль
                // браузера не всегда доступна); цикл останавливаем
                ((IJSInProcessRuntime)JsRuntime).InvokeVoid("showJsError",
                    "phase=" + _phase + "\n" + ex);
                throw;
            }
        }
    }
}
