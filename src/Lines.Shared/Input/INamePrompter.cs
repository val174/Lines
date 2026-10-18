using System;

namespace Lines.Shared.Input
{
    // Запрос имени у пользователя через платформенный диалог.
    // Реализация - в хосте (Activity1 на Android показывает AlertDialog
    // с EditText и экранной клавиатурой). На Windows не используется -
    // там ввод через Window.TextInput.
    public interface INamePrompter
    {
        // callback: введённое имя; null/пусто - отмена
        void PromptName(string title, string message, string initial,
            Action<string> callback);
    }
}
