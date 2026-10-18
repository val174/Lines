using System;

namespace Lines.Shared.Input
{
    // Источник печатных символов клавиатуры: реализуется хостом
    // (LinesGame подписывается на GameWindow.TextInput - SDL text input,
    // символы приходят с учётом раскладки, включая кириллицу).
    public interface ITextSource
    {
        event Action<char> CharTyped;
    }
}
