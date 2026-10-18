using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Lines.Shared;
using Lines.Shared.Input;
using Microsoft.Xna.Framework;

namespace Lines.Android
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.FullUser,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class Activity1 : AndroidGameActivity, INamePrompter
    {
        private LinesGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new LinesGame();
            _view = _game.Services.GetService(typeof(View)) as View;

            // Game.Exit() на Android не завершает Activity - закрываем сами
            _game.ExitRequested += OnGameExitRequested;
            // ввод имени для рекордов - нативным диалогом (см. PromptName)
            _game.NamePrompter = this;

            SetContentView(_view);
            HideSystemBars();
            _game.Run();
        }

        // системные панели возвращаются после свайпа/потери фокуса -
        // скрываем повторно при возврате фокуса
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
                HideSystemBars();
        }

        private void OnGameExitRequested()
        {
            RunOnUiThread(() => FinishAffinity());
        }

        // диалог ввода имени с экранной клавиатурой (для таблицы рекордов);
        // callback: введённое имя; null - отмена
        public void PromptName(string title, string message, string initial,
            System.Action<string> callback)
        {
            RunOnUiThread(() =>
            {
                // тема DeviceDefault - системные цвета Android (тёмная/светлая
                // тема устройства), а не дефолтный белый фон
                AlertDialog.Builder builder =
                    Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1
                        ? new AlertDialog.Builder(this, global::Android.Resource.Style.ThemeDeviceDefaultDialogAlert)
                        : new AlertDialog.Builder(this);
                EditText input = null;
                var dialog = builder
                    .SetTitle(title)
                    .SetMessage(message)
                    .SetPositiveButton(Lines.Shared.Localization.Strings.Current.Save, (s, e) => callback(input.Text))
                    .SetNegativeButton(Lines.Shared.Localization.Strings.Current.Skip, (s, e) => callback(null))
                    .Create();
                // поле ввода создаём в контексте ТЕМЫ ДИАЛОГА - иначе цвет
                // текста берётся из темы Activity и совпадает с фоном
                input = new EditText(dialog.Context) { Text = initial };
                input.SetSelection(initial?.Length ?? 0);
                dialog.SetView(input);
                // свайп назад/тап мимо диалога = отмена
                dialog.SetCanceledOnTouchOutside(false);
                dialog.CancelEvent += (s, e) => callback(null);
                // клавиатура сразу над диалогом
                dialog.Window.SetSoftInputMode(SoftInput.StateVisible);
                dialog.Show();
            });
        }

        // immersive-полноэкранный режим: скрываем строку состояния и
        // панель навигации; временный показ - полупрозрачным по свайпу
        private void HideSystemBars()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                IWindowInsetsController controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.SystemBars());
                    controller.SystemBarsBehavior =
                        (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
#pragma warning disable CS0618 // SystemUiVisibility deprecated в API 30
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                    SystemUiFlags.ImmersiveSticky |
                    SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen |
                    SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen);
#pragma warning restore CS0618
            }
        }
    }
}
