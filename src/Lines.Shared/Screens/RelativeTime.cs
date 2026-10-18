using System;
using System.Globalization;
using Lines.Shared.Localization;

namespace Lines.Shared.Screens
{
    // Относительное время ("30 минут назад" / "5 minutes ago") для дат
    // в формате "dd.MM.yy HH:mm": таблица рекордов и ячейки сохранений.
    // Строки и правила плюрализации - из Strings.Current (ru: 3 формы,
    // en: 2 формы).
    public static class RelativeTime
    {
        // минуты/часы в первые сутки, дальше - дни (до ~25), месяцы
        // (от ~25 дней, округление к ближайшему по 30 дней) и годы
        // (от ~320 дней, округление по 365); только целые
        public static string Format(string date)
        {
            if (string.IsNullOrWhiteSpace(date) ||
                !DateTime.TryParseExact(date, "dd.MM.yy HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime when))
                return date ?? "";

            Strings s = Strings.Current;
            TimeSpan span = DateTime.Now - when;
            if (span.TotalMinutes < 1)
                return s.JustNow;
            if (span.TotalHours < 1)
                return s.MinutesAgo((int)span.TotalMinutes);
            if (span.TotalDays < 1)
                return s.HoursAgo((int)span.TotalHours);
            if (span.TotalDays < 25)
                return s.DaysAgo((int)span.TotalDays);
            if (span.TotalDays < 320)
                return s.MonthsAgo((int)Math.Round(span.TotalDays / 30, MidpointRounding.AwayFromZero));
            return s.YearsAgo((int)Math.Round(span.TotalDays / 365, MidpointRounding.AwayFromZero));
        }
    }
}
